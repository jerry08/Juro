﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
/// Extractor for MegaCloud.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="MegaCloudExtractor"/>.
/// </remarks>
public class MegaCloudExtractor(IHttpClientFactory httpClientFactory) : IVideoExtractor
{
    private readonly HttpClient _http = httpClientFactory.CreateClient();

    /// <inheritdoc />
    public string ServerName => "MegaCloud";

    /// <summary>
    /// Initializes an instance of <see cref="MegaCloudExtractor"/>.
    /// </summary>
    public MegaCloudExtractor(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="MegaCloudExtractor"/>.
    /// </summary>
    public MegaCloudExtractor()
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
        var id = new Stack<string>(url.Split('/')).Pop().Split('?')[0];

        var host = new Uri(url).Host;

        var decryptKey = await DecryptKeyAsync(cancellationToken);
        if (decryptKey.Count == 0)
            return [];

        headers = new Dictionary<string, string>() { { "X-Requested-With", "XMLHttpRequest" } };

        var response = await _http.ExecuteAsync(
            $"https://{host}/embed-2/ajax/e-1/getSources?id={id}",
            headers,
            cancellationToken
        );

        var data = JsonNode.Parse(response);

        var sources = data?["sources"]?.ToString();
        if (string.IsNullOrWhiteSpace(sources))
            return [];

        var isEncrypted = (bool)data!["encrypted"]!;
        if (isEncrypted)
        {
            try
            {
                var sourcesArray = sources!.Select(x => x.ToString()).ToList();
                var extractedKey = "";
                var currentIndex = 0;

                foreach (var index in decryptKey)
                {
                    var start = index[0] + currentIndex;
                    var end = start + index[1];

                    for (var i = start; i < end; i++)
                    {
                        extractedKey += sources![i];
                        sourcesArray[i] = "";
                    }

                    currentIndex += index[1];
                }

                sources = string.Concat(sourcesArray);
                sources = sources.Trim();

                sources = Decrypt(sources, extractedKey);
            }
            catch
            {
                return [];
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

        return
        [
            new()
            {
                VideoUrl = m3u8File,
                Headers = headers,
                Format = VideoType.M3u8,
                Resolution = "Multi Quality",
                Subtitles = subtitles,
            },
        ];
    }

    public async ValueTask<List<List<int>>> DecryptKeyAsync(
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            "https://raw.githubusercontent.com/theonlymo/keys/e1/key",
            cancellationToken
        );

        if (string.IsNullOrEmpty(response))
            return [];

        return JsonSerializer.Deserialize<List<List<int>>>(response) ?? [];
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
