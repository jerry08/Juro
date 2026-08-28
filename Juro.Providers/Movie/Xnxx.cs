using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Juro.Core;
using Juro.Core.Models.Movie;
using Juro.Core.Models.Videos;
using Juro.Core.Providers;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;
using Juro.Providers.Anime;

namespace Juro.Providers.Movie;

/// <summary>
/// XNXX movie/video provider.
/// </summary>
public class Xnxx(IHttpClientFactory httpClientFactory)
    : MovieBaseProvider(httpClientFactory),
        IMovieProvider
{
    private static readonly Regex LowUrlRegex = new(
        "html5player\\.setVideoUrlLow\\(\\s*['\"](?<url>[^'\"]+)['\"]\\s*\\)",
        RegexOptions.IgnoreCase
    );

    private static readonly Regex HighUrlRegex = new(
        "html5player\\.setVideoUrlHigh\\(\\s*['\"](?<url>[^'\"]+)['\"]\\s*\\)",
        RegexOptions.IgnoreCase
    );

    private static readonly Regex HlsUrlRegex = new(
        "html5player\\.setVideoHLS\\(\\s*['\"](?<url>[^'\"]+)['\"]\\s*\\)",
        RegexOptions.IgnoreCase
    );

    private static readonly Regex IsoDurationRegex = new(
        "^PT(?:(?<h>\\d+)H)?(?:(?<m>\\d+)M)?(?:(?<s>\\d+(?:\\.\\d+)?)S)?$",
        RegexOptions.IgnoreCase
    );

    private static readonly Regex JsUnicodeEscapeRegex = new(
        "\\\\u(?<hex>[0-9a-fA-F]{4})",
        RegexOptions.Compiled
    );

    private readonly HttpClient _http = httpClientFactory.CreateClient();

    public string Key => Name;

    public string Name { get; set; } = "XNXX";

    public string Language => "en";

    public string BaseUrl { get; set; } = "https://www.xnxx.com";

    public string Logo => $"{BaseUrl}/favicon.ico";

    /// <summary>
    /// Initializes an instance of <see cref="Xnxx"/>.
    /// </summary>
    public Xnxx(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="Xnxx"/>.
    /// </summary>
    public Xnxx()
        : this(Http.ClientProvider) { }

    /// <inheritdoc />
    public ValueTask<List<MovieResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    ) => SearchAsync(query, 1, cancellationToken);

    /// <summary>
    /// Searches XNXX. Juro-facing pages are 1-based; XNXX's first page has no
    /// numeric suffix and later pages use /1, /2, ... .
    /// </summary>
    public async ValueTask<List<MovieResult>> SearchAsync(
        string query,
        int page,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var encodedQuery = Uri.EscapeDataString(query.Trim());
        var sitePage = Math.Max(0, page - 1);
        var searchUrl = $"{BaseUrl}/search/{encodedQuery}";
        if (sitePage > 0)
            searchUrl += $"/{sitePage}";

        var response = await FetchPageAsync(searchUrl, cancellationToken);
        var document = Html.Parse(response);

        // Current XNXX result cards use IDs beginning with "video_". Keep a
        // thumb-block fallback because XNXX has used that class for years.
        var nodes =
            document.DocumentNode.SelectNodes("//div[starts-with(@id, 'video_')]")
            ?? document.DocumentNode.SelectNodes(
                "//div[contains(concat(' ', normalize-space(@class), ' '), ' thumb-block ') "
                    + "and not(contains(concat(' ', normalize-space(@class), ' '), ' thumb-ad '))]"
            );

        if (nodes is null)
            return [];

        var results = new List<MovieResult>();

        foreach (var node in nodes)
        {
            var linkNode =
                node.SelectSingleNode(
                    ".//div[contains(concat(' ', normalize-space(@class), ' '), ' thumb-under ')]//p//a[@href][1]"
                )
                ?? node.SelectSingleNode(
                    ".//div[contains(concat(' ', normalize-space(@class), ' '), ' thumb-under ')]//a[@href][1]"
                )
                ?? node.SelectSingleNode(".//a[@href][1]");

            var href = linkNode?.Attributes["href"]?.Value;
            if (string.IsNullOrWhiteSpace(href))
                continue;

            var url = ToAbsoluteUrl(href);
            var title = DecodeText(linkNode?.Attributes["title"]?.Value ?? linkNode?.InnerText);

            if (string.IsNullOrWhiteSpace(title))
                continue;

            var imageNode =
                node.SelectSingleNode(
                    ".//div[contains(concat(' ', normalize-space(@class), ' '), ' thumb ')]//img[@src or @data-src][1]"
                ) ?? node.SelectSingleNode(".//img[@src or @data-src][1]");

            var image = NormalizeAssetUrl(
                imageNode?.Attributes["src"]?.Value ?? imageNode?.Attributes["data-src"]?.Value
            );

            results.Add(
                new MovieResult
                {
                    Id = GetMediaId(url),
                    Title = title,
                    Url = url,
                    Image = image,
                    Type = TvType.Movie,
                }
            );
        }

        return results;
    }

    /// <inheritdoc />
    public async ValueTask<MovieInfo> GetMediaInfoAsync(
        string mediaId,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(mediaId))
            return new();

        var pageUrl = ToAbsoluteUrl(mediaId);
        var response = await FetchPageAsync(pageUrl, cancellationToken);
        var document = Html.Parse(response);
        var ldJson = GetVideoLdJson(document);

        var title = DecodeText(
            GetJsonString(ldJson, "name")
                ?? GetMetaContent(document, "og:title")
                ?? document.DocumentNode.SelectSingleNode("//h1")?.InnerText
        );

        var image = NormalizeAssetUrl(
            GetJsonStringOrFirstArrayItem(ldJson, "thumbnailUrl")
                ?? GetMetaContent(document, "og:image")
        );

        var description = DecodeText(
            GetJsonString(ldJson, "description") ?? GetMetaContent(document, "og:description")
        );

        var releasedDate = DecodeText(GetJsonString(ldJson, "uploadDate"));
        var duration = FormatDuration(
            GetJsonString(ldJson, "duration")
                ?? GetMetaContent(document, "og:duration")
                ?? GetMetaContent(document, "duration")
        );

        var tags = GetTags(document, ldJson);
        var author =
            GetNestedJsonString(ldJson, "author", "name")
            ?? DecodeText(
                document
                    .DocumentNode.SelectSingleNode(
                        "//div[contains(concat(' ', normalize-space(@class), ' '), ' video-title-container ')]//a[contains(concat(' ', normalize-space(@class), ' '), ' gold-plate ')][1]"
                    )
                    ?.InnerText
            );

        var id = GetMediaId(pageUrl);

        return new MovieInfo
        {
            Id = id,
            Title = title,
            Image = image,
            Description = description,
            ReleasedDate = releasedDate,
            Type = TvType.Movie,
            Duration = duration,
            Tags = tags,
            Production = author,
            Episodes =
            [
                new Episode
                {
                    Id = id,
                    Title = title ?? id,
                    Number = 1,
                    Season = 1,
                    Url = pageUrl,
                },
            ],
        };
    }

    /// <inheritdoc />
    public ValueTask<List<VideoServer>> GetEpisodeServersAsync(
        string episodeId,
        string mediaId,
        CancellationToken cancellationToken = default
    )
    {
        var value = !string.IsNullOrWhiteSpace(episodeId) ? episodeId : mediaId;
        if (string.IsNullOrWhiteSpace(value))
            return new ValueTask<List<VideoServer>>([]);

        var pageUrl = ToAbsoluteUrl(value);
        return new ValueTask<List<VideoServer>>([new VideoServer(Name, pageUrl)]);
    }

    /// <inheritdoc />
    public override async ValueTask<List<VideoSource>> GetVideosAsync(
        VideoServer server,
        CancellationToken cancellationToken = default
    )
    {
        if (server?.Embed is null || string.IsNullOrWhiteSpace(server.Embed.Url))
            return [];

        var serverUrl = server.Embed.Url;
        if (!Uri.IsWellFormedUriString(serverUrl, UriKind.Absolute))
            serverUrl = ToAbsoluteUrl(serverUrl);

        if (IsDirectMediaUrl(serverUrl))
        {
            var isPlaylist = IsPlaylistUrl(serverUrl);
            return
            [
                new VideoSource
                {
                    Title = isPlaylist ? "HLS" : "Direct",
                    VideoUrl = serverUrl,
                    FileType = isPlaylist ? "m3u8" : "mp4",
                    Format = isPlaylist ? VideoType.M3u8 : VideoType.Container,
                    Headers = CreateHeaders(BaseUrl + "/"),
                    VideoServer = server,
                },
            ];
        }

        var response = await FetchPageAsync(serverUrl, cancellationToken);
        var sources = new List<VideoSource>();

        // XNXX currently exposes the HLS master URL through setVideoHLS().
        // Keep the MP4 patterns as fallbacks for layouts that still emit them.
        AddSource(sources, server, serverUrl, response, HlsUrlRegex, "HLS", "m3u8", VideoType.M3u8);
        AddSource(
            sources,
            server,
            serverUrl,
            response,
            HighUrlRegex,
            "High",
            "mp4",
            VideoType.Container
        );
        AddSource(
            sources,
            server,
            serverUrl,
            response,
            LowUrlRegex,
            "Low",
            "mp4",
            VideoType.Container
        );

        return sources;
    }

    private ValueTask<string> FetchPageAsync(string url, CancellationToken cancellationToken) =>
        _http.ExecuteAsync(
            url,
            new Dictionary<string, string> { ["Referer"] = BaseUrl + "/" },
            cancellationToken
        );

    private void AddSource(
        List<VideoSource> sources,
        VideoServer server,
        string referer,
        string html,
        Regex regex,
        string title,
        string fileType,
        VideoType format
    )
    {
        var match = regex.Match(html);
        if (!match.Success)
            return;

        var url = DecodeSourceUrl(match.Groups["url"].Value);
        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            return;

        if (sources.Any(x => string.Equals(x.VideoUrl, url, StringComparison.Ordinal)))
            return;

        sources.Add(
            new VideoSource
            {
                Title = title,
                VideoUrl = url,
                FileType = fileType,
                Format = format,
                Headers = CreateHeaders(referer),
                VideoServer = server,
            }
        );
    }

    private static Dictionary<string, string> CreateHeaders(string referer) =>
        new() { ["Referer"] = referer };

    private string ToAbsoluteUrl(string value)
    {
        value = WebUtility.HtmlDecode(value).Trim();

        if (value.StartsWith("//", StringComparison.Ordinal))
            return "https:" + value;

        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute))
            return absolute.ToString();

        return $"{BaseUrl.TrimEnd('/')}/{value.TrimStart('/')}";
    }

    private string? NormalizeAssetUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return ToAbsoluteUrl(value);
    }

    private static string GetMediaId(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return uri.AbsolutePath.TrimStart('/');

        return url.TrimStart('/');
    }

    private static string? GetMetaContent(HtmlDocument document, string key)
    {
        var node = document.DocumentNode.SelectSingleNode(
            $"//meta[@property='{key}' or @name='{key}']"
        );
        return node?.Attributes["content"]?.Value;
    }

    private static JsonObject? GetVideoLdJson(HtmlDocument document)
    {
        var scripts = document.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
        if (scripts is null)
            return null;

        foreach (var script in scripts)
        {
            if (string.IsNullOrWhiteSpace(script.InnerText))
                continue;

            try
            {
                var parsed = JsonNode.Parse(WebUtility.HtmlDecode(script.InnerText));

                if (parsed is JsonObject obj && IsVideoObject(obj))
                    return obj;

                if (parsed is JsonArray array)
                {
                    foreach (var item in array)
                    {
                        if (item is JsonObject arrayObject && IsVideoObject(arrayObject))
                            return arrayObject;
                    }
                }
            }
            catch
            {
                // Ignore malformed/unrelated JSON-LD blocks and continue.
            }
        }

        return null;
    }

    private static bool IsVideoObject(JsonObject obj)
    {
        var type = GetJsonString(obj, "@type");
        return string.Equals(type, "VideoObject", StringComparison.OrdinalIgnoreCase)
            || obj.ContainsKey("thumbnailUrl")
            || obj.ContainsKey("duration");
    }

    private static string? GetJsonString(JsonObject? obj, string key)
    {
        if (obj?[key] is not JsonValue value)
            return null;

        if (value.TryGetValue<string>(out var text))
            return text;

        return value.ToString();
    }

    private static string? GetJsonStringOrFirstArrayItem(JsonObject? obj, string key)
    {
        var node = obj?[key];
        if (node is JsonValue value)
        {
            if (value.TryGetValue<string>(out var text))
                return text;
            return value.ToString();
        }

        if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                if (item is JsonValue itemValue)
                {
                    if (itemValue.TryGetValue<string>(out var text))
                        return text;
                    return itemValue.ToString();
                }
            }
        }

        return null;
    }

    private static string? GetNestedJsonString(JsonObject? obj, string objectKey, string valueKey)
    {
        if (obj?[objectKey] is JsonObject nested)
            return GetJsonString(nested, valueKey);

        return null;
    }

    private static List<string> GetTags(HtmlDocument document, JsonObject? ldJson)
    {
        var tags =
            document
                .DocumentNode.SelectNodes(
                    "//a[contains(concat(' ', normalize-space(@class), ' '), ' is-keyword ')]"
                )
                ?.Select(x => DecodeText(x.InnerText))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToList()
            ?? [];

        var keywords = GetJsonString(ldJson, "keywords") ?? GetMetaContent(document, "keywords");
        tags.AddRange(ParseKeywords(keywords));

        return tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string? DecodeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return WebUtility.HtmlDecode(value).Trim();
    }

    private static List<string> ParseKeywords(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        return value
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(DecodeText)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? FormatDuration(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();

        if (
            long.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var seconds
            )
            && seconds >= 0
        )
        {
            return FormatTimeSpan(TimeSpan.FromSeconds(seconds));
        }

        var match = IsoDurationRegex.Match(value);
        if (!match.Success)
            return value;

        var hours = ParseDouble(match.Groups["h"].Value);
        var minutes = ParseDouble(match.Groups["m"].Value);
        var secs = ParseDouble(match.Groups["s"].Value);
        var totalSeconds = hours * 3600 + minutes * 60 + secs;

        return FormatTimeSpan(TimeSpan.FromSeconds(totalSeconds));
    }

    private static double ParseDouble(string value) =>
        string.IsNullOrWhiteSpace(value) ? 0 : double.Parse(value, CultureInfo.InvariantCulture);

    private static string FormatTimeSpan(TimeSpan duration) =>
        duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
            : $"{duration.Minutes}:{duration.Seconds:00}";

    private static string DecodeSourceUrl(string value)
    {
        var decoded = WebUtility.HtmlDecode(value).Replace("\\/", "/");
        decoded = JsUnicodeEscapeRegex.Replace(
            decoded,
            match =>
            {
                var code = int.Parse(
                    match.Groups["hex"].Value,
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture
                );
                return char.ConvertFromUtf32(code);
            }
        );

        return decoded.Trim();
    }

    private static bool IsDirectMediaUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var path = uri.AbsolutePath;
        return path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPlaylistUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.AbsolutePath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase);
    }
}
