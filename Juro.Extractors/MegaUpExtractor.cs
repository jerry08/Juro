using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
/// Extractor for MegaUp video servers used by AniKai/AnimeKai.
/// Uses the enc-dec.app API for token generation and decryption.
/// </summary>
public class MegaUpExtractor(IHttpClientFactory httpClientFactory) : IVideoExtractor
{
    private readonly HttpClient _http = httpClientFactory.CreateClient();

    private const string ApiBase = "https://enc-dec.app/api";
    private const string DefaultUserAgent =
        "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Mobile Safari/537.36";

    /// <inheritdoc />
    public string ServerName => "MegaUp";

    /// <summary>
    /// Initializes an instance of <see cref="MegaUpExtractor"/>.
    /// </summary>
    public MegaUpExtractor(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="MegaUpExtractor"/>.
    /// </summary>
    public MegaUpExtractor()
        : this(Http.ClientProvider) { }

    /// <summary>
    /// Generates an encrypted token for use in AniKai AJAX requests.
    /// </summary>
    public async ValueTask<string> GenerateTokenAsync(
        string text,
        CancellationToken cancellationToken = default
    ) => await GenerateTokenAsync(text, null, cancellationToken);

    /// <summary>
    /// Generates an encrypted token for use in AniKai AJAX requests.
    /// </summary>
    public async ValueTask<string> GenerateTokenAsync(
        string text,
        string? origin,
        CancellationToken cancellationToken = default
    )
    {
        var url = $"{ApiBase}/enc-kai?text={Uri.EscapeDataString(text)}";
        var response = await _http.ExecuteAsync(
            url,
            CreateEncDecHeaders(origin),
            cancellationToken
        );
        var json = JsonNode.Parse(response);
        return json?["result"]?.ToString() ?? "";
    }

    /// <summary>
    /// Decodes encrypted iframe data to get the video URL and skip timings.
    /// </summary>
    public async ValueTask<(string Url, int[] Intro, int[] Outro)> DecodeIframeDataAsync(
        string text,
        CancellationToken cancellationToken = default
    ) => await DecodeIframeDataAsync(text, null, cancellationToken);

    /// <summary>
    /// Decodes encrypted iframe data to get the video URL and skip timings.
    /// </summary>
    public async ValueTask<(string Url, int[] Intro, int[] Outro)> DecodeIframeDataAsync(
        string text,
        string? origin,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/dec-kai");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { text }),
            Encoding.UTF8,
            "application/json"
        );
        AddHeaders(request, CreateEncDecHeaders(origin));

        var response = await _http.ExecuteAsync(request, cancellationToken);
        var json = JsonNode.Parse(response);
        var result = json?["result"];

        var videoUrl = result?["url"]?.ToString() ?? "";
        var intro = ParseSkipArray(result?["skip"]?["intro"]);
        var outro = ParseSkipArray(result?["skip"]?["outro"]);

        return (videoUrl, intro, outro);
    }

    private static int[] ParseSkipArray(JsonNode? node)
    {
        if (node is JsonArray arr && arr.Count >= 2)
        {
            return [arr[0]?.GetValue<int>() ?? 0, arr[1]?.GetValue<int>() ?? 0];
        }
        return [0, 0];
    }

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
        var userAgent = GetHeaderValue(headers, "User-Agent") ?? DefaultUserAgent;
        var referer = GetHeaderValue(headers, "Referer") ?? url;

        // Transform /e/ to /media/ to get the encrypted sources
        var mediaUrl = url.Replace("/e/", "/media/");
        var mediaRequest = new HttpRequestMessage(HttpMethod.Get, mediaUrl);
        AddHeaders(
            mediaRequest,
            new Dictionary<string, string>
            {
                ["User-Agent"] = userAgent,
                ["Accept"] = "application/json, text/plain, */*",
                ["X-Requested-With"] = "XMLHttpRequest",
                ["Referer"] = url,
            }
        );

        var response = await _http.ExecuteAsync(mediaRequest, cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
            return [];

        var json = JsonNode.Parse(response);
        var encryptedData = json?["result"]?.ToString();

        if (string.IsNullOrWhiteSpace(encryptedData))
            return [];

        // Decrypt via the dec-mega API — must use the same User-Agent as the /media/ request
        var decRequest = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/dec-mega");
        decRequest.Content = new StringContent(
            JsonSerializer.Serialize(new { text = encryptedData, agent = userAgent }),
            Encoding.UTF8,
            "application/json"
        );
        AddHeaders(decRequest, CreateEncDecHeaders(referer, userAgent));

        var decResponse = await _http.ExecuteAsync(decRequest, cancellationToken);
        var decJson = JsonNode.Parse(decResponse);
        var result = decJson?["result"];

        if (result is null)
            return [];

        var videos = new List<VideoSource>();

        // Parse sources
        var sources = result["sources"]?.AsArray();
        if (sources is not null)
        {
            var megaHost = new Uri(url).GetLeftPart(UriPartial.Authority);
            var videoHeaders = new Dictionary<string, string>
            {
                ["User-Agent"] = userAgent,
                ["Origin"] = megaHost,
                ["Referer"] = $"{megaHost}/",
            };

            foreach (var source in sources)
            {
                var file = source?["file"]?.ToString();
                if (string.IsNullOrWhiteSpace(file))
                    continue;

                var isM3u8 = file!.Contains(".m3u8");
                videos.Add(
                    new VideoSource
                    {
                        VideoUrl = file,
                        Format = isM3u8 ? VideoType.M3u8 : VideoType.Container,
                        Resolution = isM3u8 ? "Multi Quality" : "Default",
                        Headers = videoHeaders,
                    }
                );
            }
        }

        // Parse subtitles/tracks
        var tracks = result["tracks"]?.AsArray();
        if (tracks is not null && videos.Count > 0)
        {
            var subtitles = new List<Subtitle>();
            foreach (var track in tracks)
            {
                var kind = track?["kind"]?.ToString();
                if (kind != "captions" && kind != "subtitles")
                    continue;

                var file = track?["file"]?.ToString();
                var label = track?["label"]?.ToString() ?? "Unknown";

                if (!string.IsNullOrWhiteSpace(file))
                {
                    subtitles.Add(new Subtitle(file!, label));
                }
            }

            foreach (var video in videos)
            {
                video.Subtitles = subtitles;
            }
        }

        return videos;
    }

    private static Dictionary<string, string> CreateEncDecHeaders(
        string? originOrReferer,
        string? userAgent = null
    )
    {
        var origin = GetOrigin(originOrReferer) ?? "https://animekai.to";

        return new Dictionary<string, string>
        {
            ["User-Agent"] = userAgent ?? DefaultUserAgent,
            ["Accept"] = "application/json, text/plain, */*",
            ["Origin"] = origin,
            ["Referer"] = $"{origin}/watch",
            ["Sec-Fetch-Dest"] = "empty",
            ["Sec-Fetch-Mode"] = "cors",
            ["Sec-Fetch-Site"] = "cross-site",
        };
    }

    private static string? GetOrigin(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        return uri.GetLeftPart(UriPartial.Authority);
    }

    private static string? GetHeaderValue(Dictionary<string, string> headers, string key)
    {
        foreach (var header in headers)
        {
            if (string.Equals(header.Key, key, StringComparison.OrdinalIgnoreCase))
                return header.Value;
        }

        return null;
    }

    private static void AddHeaders(HttpRequestMessage request, Dictionary<string, string> headers)
    {
        foreach (var header in headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }
}
