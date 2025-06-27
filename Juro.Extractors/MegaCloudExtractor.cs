using System;
using System.Collections.Generic;
using System.Diagnostics;
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

// Inspired from https://github.com/yuzono/aniyomi-extensions/blob/master/lib/megacloud-extractor/src/main/java/eu/kanade/tachiyomi/lib/megacloudextractor/MegaCloudExtractor.kt
/// <summary>
/// Extractor for MegaCloud.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="MegaCloudExtractor"/>.
/// </remarks>
public class MegaCloudExtractor(IHttpClientFactory httpClientFactory) : IVideoExtractor
{
    private readonly HttpClient _http = httpClientFactory.CreateClient();

    private static readonly string[] SOURCES_URL =
    [
        "/embed-2/v2/e-1/getSources?id=",
        "/ajax/embed-6-v2/getSources?id=",
    ];
    private static readonly string[] SOURCES_SPLITTER = ["/e-1/", "/embed-6-v2/"];
    private static readonly string[] SERVER_URL =
    [
        "https://megacloud.tv",
        "https://rapid-cloud.co",
    ];
    private static readonly string[] SOURCES_KEY = ["1", "6"];

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
        var type =
            url.StartsWith("https://megacloud.tv") || url.StartsWith("https://megacloud.blog")
                ? 0
                : 1;

        var id = url.Substring(url.IndexOf(SOURCES_SPLITTER[type]) + SOURCES_SPLITTER[type].Length);
        id = id.Substring(0, id.IndexOf("?"));
        if (string.IsNullOrEmpty(id))
        {
            throw new Exception("Failed to extract ID from URL");
        }

        var srcRes = await _http.ExecuteAsync(
            SERVER_URL[type] + SOURCES_URL[type] + id,
            headers,
            cancellationToken
        );

        var data = JsonNode.Parse(srcRes);

        var sources = data?["sources"]?.ToString();
        if (string.IsNullOrWhiteSpace(sources))
            return [];

        var isEncrypted = (bool)data!["encrypted"]!;
        if (isEncrypted)
        {
            sources = await TryDecryptingAsync(sources);
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

    private string? megaKey;

    public async Task<string> TryDecryptingAsync(string ciphered)
    {
        if (megaKey != null)
        {
            try
            {
                var decryptedUrl = DecryptOpenSSL(ciphered, megaKey);
                Console.WriteLine($"Decrypted URL: {decryptedUrl}");
                return decryptedUrl;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Decryption failed with existing key: {ex.Message}");
                return await DecryptWithNewKeyAsync(ciphered);
            }
        }
        else
        {
            return await DecryptWithNewKeyAsync(ciphered);
        }
    }

    private async Task<string> DecryptWithNewKeyAsync(string ciphered)
    {
        var newKey = await RequestNewKeyAsync();
        megaKey = newKey;
        var decryptedUrl = DecryptOpenSSL(ciphered, newKey);
        Console.WriteLine($"Decrypted URL with new key: {decryptedUrl}");
        return decryptedUrl;
    }

    private async Task<string> RequestNewKeyAsync()
    {
        try
        {
            var response = await _http.GetAsync(
                "https://raw.githubusercontent.com/yogesh-hacker/MegacloudKeys/refs/heads/main/keys.json"
            );
            response.EnsureSuccessStatusCode();

            var jsonStr = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(jsonStr))
                throw new InvalidOperationException("keys.json is empty");

            var json = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonStr);
            if (json == null || !json.ContainsKey("mega"))
                throw new InvalidOperationException("Mega key not found in keys.json");

            var key = json["mega"];
            Console.WriteLine($"Using Mega Key: {key}");
            megaKey = key;
            return key;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to fetch keys.json: {ex.Message}");
            throw;
        }
    }

    private static string DecryptOpenSSL(string encBase64, string password)
    {
        try
        {
            var data = Convert.FromBase64String(encBase64);
            if (!data.Take(8).SequenceEqual(Encoding.ASCII.GetBytes("Salted__")))
            {
                throw new InvalidOperationException("Invalid encrypted data format.");
            }

            var salt = data.Skip(8).Take(8).ToArray();
            var (key, iv) = OpenSSLKeyIv(Encoding.UTF8.GetBytes(password), salt);

            using var cipher = Aes.Create();
            cipher.Mode = CipherMode.CBC;
            cipher.Padding = PaddingMode.PKCS7;
            cipher.Key = key;
            cipher.IV = iv;

            using var decryptor = cipher.CreateDecryptor();
            using var ms = new MemoryStream(data.Skip(16).ToArray());
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Decryption failed: {ex.Message}");
            throw new InvalidOperationException($"Decryption failed: {ex.Message}", ex);
        }
    }

    private static (byte[] key, byte[] iv) OpenSSLKeyIv(
        byte[] password,
        byte[] salt,
        int keyLen = 32,
        int ivLen = 16
    )
    {
        var d = new byte[0];
        var d_i = new byte[0];
        while (d.Length < keyLen + ivLen)
        {
            using var md = MD5.Create();
            d_i = md.ComputeHash(Combine(d_i, password, salt));
            d = Combine(d, d_i);
        }

        var key = new byte[keyLen];
        var iv = new byte[ivLen];
        Array.Copy(d, 0, key, 0, keyLen);
        Array.Copy(d, keyLen, iv, 0, ivLen);

        return (key, iv);
    }

    private static byte[] Combine(params byte[][] arrays)
    {
        var length = arrays.Sum(a => a.Length);
        var result = new byte[length];
        var offset = 0;
        foreach (var array in arrays)
        {
            Buffer.BlockCopy(array, 0, result, offset, array.Length);
            offset += array.Length;
        }
        return result;
    }
}
