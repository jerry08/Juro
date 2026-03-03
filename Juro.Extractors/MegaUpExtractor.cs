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
    )
    {
        var url = $"{ApiBase}/enc-kai?text={Uri.EscapeDataString(text)}";
        var response = await _http.ExecuteAsync(url, cancellationToken);
        var json = JsonNode.Parse(response);
        return json?["result"]?.ToString() ?? "";
    }

    /// <summary>
    /// Decodes encrypted iframe data to get the video URL and skip timings.
    /// </summary>
    public async ValueTask<(string Url, int[] Intro, int[] Outro)> DecodeIframeDataAsync(
        string text,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/dec-kai");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { text }),
            Encoding.UTF8,
            "application/json"
        );

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
        // Use a consistent User-Agent for both /media/ and dec-mega requests
        var userAgent = Http.ChromeUserAgent();

        // Transform /e/ to /media/ to get the encrypted sources
        var mediaUrl = url.Replace("/e/", "/media/");
        var mediaRequest = new HttpRequestMessage(HttpMethod.Get, mediaUrl);
        mediaRequest.Headers.TryAddWithoutValidation("User-Agent", userAgent);

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
                        Headers = new Dictionary<string, string>
                        {
                            ["Referer"] = new Uri(url).GetLeftPart(UriPartial.Authority),
                        },
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
}
