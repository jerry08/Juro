using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
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
/// <remarks>
/// Initializes an instance of <see cref="RapidCloudExtractor"/>.
/// </remarks>
public class RapidCloudExtractor(IHttpClientFactory httpClientFactory) : IVideoExtractor
{
    private readonly HttpClient _http = httpClientFactory.CreateClient();
    private string? _cachedKey;

    private const string SourcesUrl = "/embed-2/v2/e-1/getSources?id=";
    private const string SourcesSplitter = "/e-1/";

    /// <inheritdoc />
    public string ServerName => "RapidCloud";

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

    /// <inheritdoc />
    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default
    ) => await ExtractAsync(url, [], cancellationToken);

    /// <inheritdoc />
    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var uri = new Uri(url);
        var serverUrl = uri.GetLeftPart(UriPartial.Authority);
        var id = url.Split([SourcesSplitter], StringSplitOptions.None)
            .LastOrDefault()
            ?.Split('?')
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(id))
            id = new Stack<string>(url.Split('/')).Pop().Split('?')[0];

        if (string.IsNullOrWhiteSpace(id))
            return [];

        headers = new Dictionary<string, string>(headers)
        {
            ["Accept"] = "*/*",
            ["X-Requested-With"] = "XMLHttpRequest",
            ["Referer"] = $"{serverUrl}/",
        };

        var response = await _http.ExecuteAsync(
            $"{serverUrl}{SourcesUrl}{id}",
            headers,
            cancellationToken
        );

        var data = JsonNode.Parse(response);

        if (data?["sources"] is not JsonArray sources)
            return [];

        var isEncrypted = data["encrypted"]?.GetValue<bool>() ?? true;
        var key = isEncrypted ? _cachedKey ?? await RequestNewKeyAsync(cancellationToken) : "";

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

        var videoSources = new List<VideoSource>();
        foreach (var source in sources)
        {
            var file = source?["file"]?.ToString();
            if (string.IsNullOrWhiteSpace(file))
                continue;

            var m3u8File =
                isEncrypted && !file.Contains(".m3u8", StringComparison.OrdinalIgnoreCase)
                    ? Decrypt(file, key)
                    : file;

            videoSources.Add(
                new()
                {
                    VideoUrl = m3u8File,
                    Headers = headers,
                    Format = VideoType.M3u8,
                    Resolution = "Multi Quality",
                    Subtitles = subtitles,
                }
            );
        }

        return videoSources;
    }

    private async Task<string> RequestNewKeyAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync(
            "https://raw.githubusercontent.com/yogesh-hacker/MegacloudKeys/refs/heads/main/keys.json",
            cancellationToken
        );

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        var data = JsonNode.Parse(json);
        var key = data?["mega"]?.ToString();

        if (string.IsNullOrWhiteSpace(key))
            throw new Exception("Rapid key not found in keys.json");

        _cachedKey = key;
        return _cachedKey;
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
