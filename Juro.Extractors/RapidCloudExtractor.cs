using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core;
using Juro.Core.Models;
using Juro.Core.Models.Videos;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;

namespace Juro.Extractors;

/// <summary>
/// Extractor for RapidCloud.
/// </summary>
public class RapidCloudExtractor : IVideoExtractor
{
    private readonly HttpClient _http;

    /// <inheritdoc />
    public string ServerName => "RapidCloud";

    /// <summary>
    /// Initializes an instance of <see cref="RapidCloudExtractor"/>.
    /// </summary>
    public RapidCloudExtractor(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient();
    }

    /// <summary>
    /// Initializes an instance of <see cref="RapidCloudExtractor"/>.
    /// </summary>
    public RapidCloudExtractor(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="RapidCloudExtractor"/>.
    /// </summary>
    public RapidCloudExtractor()
        : this(Http.ClientProvider) { }

    private async Task<List<(int Value1, int Value2)>> GetIndexPairsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var script = await _http.ExecuteAsync(
            $"https://rapid-cloud.co/js/player/prod/e6-player-v2.min.js",
            cancellationToken
        );

        var regex = new Regex(
            "case\\s*0x[0-9a-f]+:(?![^;]*=partKey)\\s*\\w+\\s*=\\s*(\\w+)\\s*,\\s*\\w+\\s*=\\s*(\\w+);"
        );
        var matches = regex.Matches(script).OfType<Match>().ToList();

        var list = new List<(int, int)>();

        foreach (var match in matches)
        {
            var var1 = match.Groups[1].Value;
            var var2 = match.Groups[2].Value;

            var regexVar1 = new Regex($",{var1}=((?:0x)?([0-9a-fA-F]+))");
            var regexVar2 = new Regex($",{var2}=((?:0x)?([0-9a-fA-F]+))");

            var matchVar1 = regexVar1
                .Match(script)
                ?.Groups.OfType<Group>()
                .ElementAtOrDefault(1)
                ?.Value?.RemovePrefix("0x");
            var matchVar2 = regexVar2
                .Match(script)
                ?.Groups.OfType<Group>()
                .ElementAtOrDefault(1)
                ?.Value;

            if (matchVar1 is not null && matchVar2 is not null)
            {
                try
                {
                    list.Add(new(Convert.ToByte(matchVar1, 16), Convert.ToByte(matchVar2, 16)));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }

        return list;
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default
    )
    {
        var id = new Stack<string>(url.Split('/')).Pop().Split('?')[0];

        var host = new Uri(url).Host;

        var indexPairs = await GetIndexPairsAsync(cancellationToken);
        if (indexPairs.Count == 0)
            return new();

        var headers = new Dictionary<string, string>() { { "X-Requested-With", "XMLHttpRequest" } };

        var response = await _http.ExecuteAsync(
            $"https://{host}/ajax/embed-6-v2/getSources?id={id}",
            headers,
            cancellationToken
        );

        var data = JsonNode.Parse(response);

        var sources = data?["sources"]?.ToString();
        if (string.IsNullOrWhiteSpace(sources))
            return new();

        var isEncrypted = (bool)data!["encrypted"]!;
        if (isEncrypted)
        {
            try
            {
                var sourcesArray = sources!.Select(x => x.ToString()).ToList();
                var extractedKey = "";
                var currentIndex = 0;

                foreach (var index in indexPairs)
                {
                    var start = index.Value1 + currentIndex;
                    var end = start + index.Value2;

                    for (var i = start; i < end; i++)
                    {
                        extractedKey += sources![i];
                        sourcesArray[i] = "";
                    }

                    currentIndex += index.Value2;
                }

                sources = string.Concat(sourcesArray);
                sources = sources.Trim();

                sources = Decrypt(sources, extractedKey);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new();
            }
        }

        var subtitles = new List<Subtitle>();

        var tracksStr = data["tracks"]?.ToString();
        if (!string.IsNullOrWhiteSpace(tracksStr))
        {
            foreach (var subtitle in JsonNode.Parse(tracksStr!)!.AsArray())
            {
                var kind = subtitle!["kind"]?.ToString();
                var label = subtitle["label"]?.ToString();
                var file = subtitle["file"]?.ToString();

                if (
                    kind == "captions"
                    && !string.IsNullOrEmpty(label)
                    && !string.IsNullOrEmpty(file)
                )
                {
                    subtitles.Add(new(file!, label!));
                }
            }
        }

        var m3u8File = JsonNode.Parse(sources!)![0]!["file"]!.ToString();

        return new List<VideoSource>
        {
            new()
            {
                VideoUrl = m3u8File,
                Headers = headers,
                Format = VideoType.M3u8,
                Resolution = "Multi Quality",
                Subtitles = subtitles
            }
        };
    }

    private static byte[] Md5(byte[] inputBytes) => MD5.Create().ComputeHash(inputBytes);

    private static byte[] GenerateKey(byte[] salt, byte[] secret)
    {
        var key = Md5(secret.Concat(salt).ToArray());
        var currentKey = key;
        while (currentKey.Length < 48)
        {
            key = Md5(key.Concat(secret).Concat(salt).ToArray());
            currentKey = currentKey.Concat(key).ToArray();
        }
        return currentKey;
    }

    private static string Decrypt(string input, string key) =>
        DecryptSourceUrl(
            GenerateKey(
                input.DecodeBase64ToBytes().CopyOfRange(8, 16),
                Encoding.UTF8.GetBytes(key)
            ),
            input
        );

    private static string DecryptSourceUrl(byte[] decryptionKey, string sourceUrl)
    {
        var cipherData = sourceUrl.DecodeBase64ToBytes();
        var encrypted = cipherData.CopyOfRange(16, cipherData.Length);

        var keyBytes = decryptionKey.CopyOfRange(0, 32);
        var ivBytes = decryptionKey.CopyOfRange(32, decryptionKey.Length);

        var aes = Aes.Create();
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Create a MemoryStream
        var ms = new MemoryStream(encrypted, 0, encrypted.Length);

        // Create a CryptoStream that decrypts the data
        var cs = new CryptoStream(
            ms,
            aes.CreateDecryptor(keyBytes, ivBytes),
            CryptoStreamMode.Read
        );

        // Read the Crypto Stream
        var sr = new StreamReader(cs, Encoding.ASCII);

        return sr.ReadToEnd();
    }
}
