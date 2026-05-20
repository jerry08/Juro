using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Juro.Core;
using Juro.Core.Models;
using Juro.Core.Models.Anime;
using Juro.Core.Models.Videos;
using Juro.Core.Providers;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;
using Juro.Core.Utils.Tasks;
using Juro.Extractors;

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with Aniwave.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="Aniwave"/>.
/// </remarks>
public class Aniwave(IHttpClientFactory httpClientFactory)
    : AnimeBaseProvider(httpClientFactory),
        IAnimeProvider,
        IPopularProvider,
        ILastUpdatedProvider
{
    private static readonly Regex _episodeUrlSuffixRegex = new(
        @"/ep-\d+(?:\.\d+)?$",
        RegexOptions.Compiled
    );
    private static readonly Regex _softSubRegex = new(
        @"\bsoftsub\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private readonly HttpClient _http = httpClientFactory.CreateClient();
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public string Key => Name;

    public string Name => "Aniwave";

    public string Language => "en";

    public bool IsDubAvailableSeparately => true;

    public string BaseUrl => "https://animewave.to";

    /// <summary>
    /// Initializes an instance of <see cref="Aniwave"/>.
    /// </summary>
    public Aniwave(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="Aniwave"/>.
    /// </summary>
    public Aniwave()
        : this(Http.ClientProvider) { }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        var vrf = string.IsNullOrWhiteSpace(query) ? string.Empty : VrfEncrypt(query);
        var url = $"{BaseUrl}/filter?keyword={Uri.EscapeDataString(query)}&page=1&vrf={vrf}";
        var response = await _http.ExecuteAsync(url, BuildHeaders(BaseUrl), cancellationToken);
        return ParseAnimeResponse(response);
    }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> GetPopularAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/most-viewed/?page={page}",
            BuildHeaders(BaseUrl),
            cancellationToken
        );

        return ParseAnimeResponse(response);
    }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> GetLastUpdatedAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/latest-updated/?page={page}",
            BuildHeaders(BaseUrl),
            cancellationToken
        );

        return ParseAnimeResponse(response);
    }

    /// <inheritdoc />
    public async ValueTask<IAnimeInfo> GetAnimeInfoAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var animePath = NormalizeAnimePath(id);
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}{animePath}",
            BuildHeaders(BaseUrl),
            cancellationToken
        );
        var document = Html.Parse(response);
        var titleNode = document.DocumentNode.SelectSingleNode(
            "//h1[contains(@class, 'title')] | //h2[contains(@class, 'title')]"
        );
        var animeId =
            document
                .DocumentNode.SelectSingleNode("//*[@data-id]")
                ?.GetAttributeValue("data-id", string.Empty)
            ?? document
                .DocumentNode.SelectSingleNode("//*[@data-tip]")
                ?.GetAttributeValue("data-tip", string.Empty);
        var imageNode = document.DocumentNode.SelectSingleNode(
            "//div[contains(@class, 'poster')]//img"
        );

        var anime = new AnimeInfo
        {
            Id = string.IsNullOrWhiteSpace(animeId) ? animePath : $"{animePath}#{animeId}",
            Site = AnimeSites.Aniwave,
            Title = GetTitle(titleNode),
            OtherNames = titleNode?.GetAttributeValue("data-jp", string.Empty),
            Image = DefaultIfBlank(
                imageNode?.GetAttributeValue("data-src", string.Empty),
                imageNode?.GetAttributeValue("src", string.Empty)
            ),
            Summary = document
                .DocumentNode.SelectSingleNode(
                    "//div[contains(@class, 'synopsis')]//div[contains(@class, 'content')] | //div[@class='content']"
                )
                ?.InnerText.Trim(),
            Link = $"{BaseUrl}{animePath}",
        };

        anime.Genres =
            document
                .DocumentNode.SelectNodes("//div[contains(., 'Genres')]/span/a")
                ?.Select(x => new Genre(x.InnerText.Trim()))
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .ToList()
            ?? [];
        anime.Category = SelectMetaValue(document, "Studios");
        anime.Status = SelectMetaValue(document, "Status");
        anime.Released = SelectMetaValue(document, "Aired");
        anime.Type = SelectMetaValue(document, "Type");

        return anime;
    }

    /// <inheritdoc />
    public async ValueTask<List<Episode>> GetEpisodesAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var animePath = NormalizeAnimePath(id);
        var animeId = ExtractFragment(id);
        if (string.IsNullOrWhiteSpace(animeId))
            animeId = await ResolveAnimeIdAsync(animePath, cancellationToken);

        if (string.IsNullOrWhiteSpace(animeId))
            return [];

        var referer = $"{BaseUrl}{animePath}";
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/ajax/episode/list/{animeId}?vrf={VrfEncrypt(animeId!)}",
            BuildAjaxHeaders(referer),
            cancellationToken
        );
        var html = JsonNode.Parse(response)?["result"]?.ToString();
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var document = Html.Parse(html!);
        var nodes = document.DocumentNode.SelectNodes(
            "//div[contains(@class, 'episodes')]//ul/li/a"
        );
        if (nodes is null)
            return [];

        return nodes.Select(node => ParseEpisode(node, animePath)).Reverse().ToList();
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    )
    {
        var ids = Before(episodeId, "&");
        if (string.IsNullOrWhiteSpace(ids))
            ids = episodeId;

        var epUrl = Before(After(episodeId, "epurl="), "&");
        if (string.IsNullOrWhiteSpace(epUrl))
            epUrl = BaseUrl;

        var referer = epUrl.StartsWith("http") ? epUrl : $"{BaseUrl}{epUrl}";
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/ajax/server/list?servers={Uri.EscapeDataString(ids)}",
            BuildAjaxHeaders(referer),
            cancellationToken
        );
        var html = JsonNode.Parse(response)?["result"]?.ToString();
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var document = Html.Parse(html!);
        var nodes = document
            .DocumentNode.SelectNodes(
                "//div[contains(@class, 'servers')]/div[contains(@class, 'type')]/ul/li"
            )
            ?.ToList();
        if (nodes is null)
            return [];

        var functions = nodes.Select(node =>
            (Func<Task<VideoServer?>>)(
                async () => await GetVideoServerAsync(node, referer, cancellationToken)
            )
        );

        return (await TaskEx.Run(functions, 10))
            .Where(x => x is not null)
            .Cast<VideoServer>()
            .ToList();
    }

    /// <inheritdoc />
    public override async ValueTask<List<VideoSource>> GetVideosAsync(
        VideoServer server,
        CancellationToken cancellationToken = default
    )
    {
        var lowerName = server.Name.ToLowerInvariant();
        if (
            lowerName.IndexOf("vidstream", StringComparison.Ordinal) >= 0
            || lowerName.IndexOf("vidcloud", StringComparison.Ordinal) >= 0
            || lowerName.IndexOf("megaplay", StringComparison.Ordinal) >= 0
        )
        {
            return await ExtractFromPlayerAsync(server, cancellationToken);
        }

        return await base.GetVideosAsync(server, cancellationToken);
    }

    public override IVideoExtractor? GetVideoExtractor(VideoServer server)
    {
        var lowerName = server.Name.ToLowerInvariant();
        if (lowerName.IndexOf("filemoon", StringComparison.Ordinal) >= 0)
            return new FilemoonExtractor(_httpClientFactory);
        if (lowerName.IndexOf("streamtape", StringComparison.Ordinal) >= 0)
            return new StreamTapeExtractor(_httpClientFactory);
        if (lowerName.IndexOf("mp4upload", StringComparison.Ordinal) >= 0)
            return new Mp4uploadExtractor(_httpClientFactory);

        return base.GetVideoExtractor(server);
    }

    private async ValueTask<List<VideoSource>> ExtractFromPlayerAsync(
        VideoServer server,
        CancellationToken cancellationToken
    )
    {
        if (!Uri.IsWellFormedUriString(server.Embed.Url, UriKind.Absolute))
            return [];

        var parentUrl = server.Embed.Url;
        var embedUrl = await ResolveEmbedChainAsync(parentUrl, cancellationToken);
        if (!Uri.TryCreate(embedUrl, UriKind.Absolute, out var uri))
            return [];

        var userAgent = Http.ChromeUserAgent();
        var pageBody = await _http.ExecuteAsync(
            embedUrl,
            new Dictionary<string, string> { ["Referer"] = parentUrl, ["User-Agent"] = userAgent },
            cancellationToken
        );
        var dataId = Regex.Match(pageBody, "data-id=\"(?<id>[^\"]+)\"").Groups["id"].Value;
        if (string.IsNullOrWhiteSpace(dataId))
            return [];

        var origin = uri.GetLeftPart(UriPartial.Authority);
        var sourceHeaders = new Dictionary<string, string>
        {
            ["Accept"] = "*/*",
            ["User-Agent"] = userAgent,
            ["X-Requested-With"] = "XMLHttpRequest",
            ["Referer"] = embedUrl,
            ["Origin"] = origin,
        };
        var playbackHeaders = new Dictionary<string, string>
        {
            ["Accept"] = "*/*",
            ["User-Agent"] = userAgent,
            ["Referer"] = $"{origin}/",
        };
        var sourcesBody = await _http.ExecuteAsync(
            $"{origin}/stream/getSources?id={Uri.EscapeDataString(dataId)}",
            sourceHeaders,
            cancellationToken
        );
        var data = JsonNode.Parse(sourcesBody) as JsonObject;
        var videoUrl = ExtractVideoUrl(data?["sources"]);
        if (string.IsNullOrWhiteSpace(videoUrl))
            return [];

        var subtitles = ParsePlayerSubtitles(data?["tracks"] as JsonArray);
        var isDash = ContainsIgnoreCase(videoUrl!, ".mpd");
        return
        [
            new VideoSource
            {
                Title = server.Name,
                Resolution = "Multi Quality",
                VideoUrl = videoUrl!,
                Format = isDash ? VideoType.Dash : VideoType.M3u8,
                FileType = isDash ? "mpd" : "m3u8",
                Headers = playbackHeaders,
                Subtitles = subtitles,
                VideoServer = server,
            },
        ];
    }

    private async ValueTask<string> ResolveEmbedChainAsync(
        string url,
        CancellationToken cancellationToken
    )
    {
        var currentUrl = url;
        for (var i = 0; i < 3; i++)
        {
            var response = await _http.ExecuteAsync(
                currentUrl,
                new Dictionary<string, string> { ["Referer"] = BaseUrl },
                cancellationToken
            );
            var iframe = Html.Parse(response)
                .DocumentNode.SelectSingleNode("//iframe[@src]")
                ?.GetAttributeValue("src", string.Empty);
            if (string.IsNullOrWhiteSpace(iframe))
                return currentUrl;

            currentUrl = NormalizeUrl(iframe!, currentUrl);
        }

        return currentUrl;
    }

    private List<IAnimeInfo> ParseAnimeResponse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return [];

        var document = Html.Parse(response!);
        var nodes = document.DocumentNode.SelectNodes(
            "//div[contains(@class, 'ani') and contains(@class, 'items')]/div[contains(@class, 'item')]"
        );
        if (nodes is null)
            return [];

        return nodes.Select(ParseAnimeCard).Where(x => x is not null).Cast<IAnimeInfo>().ToList();
    }

    private AnimeInfo? ParseAnimeCard(HtmlNode node)
    {
        var linkNode = node.SelectSingleNode(".//a[contains(@class, 'name')]");
        var href = Before(linkNode?.GetAttributeValue("href", string.Empty), "?");
        if (string.IsNullOrWhiteSpace(href))
            return null;

        var path = _episodeUrlSuffixRegex.Replace(href!, string.Empty);
        var imageNode = node.SelectSingleNode(".//div[contains(@class, 'poster')]//img");

        return new AnimeInfo
        {
            Id = path,
            Site = AnimeSites.Aniwave,
            Title = GetTitle(linkNode),
            OtherNames = linkNode?.GetAttributeValue("data-jp", string.Empty),
            Image = DefaultIfBlank(
                imageNode?.GetAttributeValue("data-src", string.Empty),
                imageNode?.GetAttributeValue("src", string.Empty)
            ),
            Link = $"{BaseUrl}{path}",
        };
    }

    private async ValueTask<string> ResolveAnimeIdAsync(
        string animePath,
        CancellationToken cancellationToken
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}{animePath}",
            BuildHeaders(BaseUrl),
            cancellationToken
        );
        var document = Html.Parse(response);
        return document
                .DocumentNode.SelectSingleNode("//*[@data-id]")
                ?.GetAttributeValue("data-id", string.Empty)
            ?? document
                .DocumentNode.SelectSingleNode("//*[@data-tip]")
                ?.GetAttributeValue("data-tip", string.Empty)
            ?? string.Empty;
    }

    private Episode ParseEpisode(HtmlNode node, string animePath)
    {
        var title = node.ParentNode?.GetAttributeValue("title", string.Empty) ?? string.Empty;
        var numberString = node.GetAttributeValue("data-num", "0");
        var number = float.TryParse(
            numberString,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var parsed
        )
            ? parsed
            : 0;
        var ids = node.GetAttributeValue("data-ids", string.Empty);
        var sub = node.GetAttributeValue("data-sub", string.Empty) == "1" ? "Sub" : string.Empty;
        var dub = node.GetAttributeValue("data-dub", string.Empty) == "1" ? "Dub" : string.Empty;
        var softSub = _softSubRegex.IsMatch(title) ? "SoftSub" : string.Empty;
        var episodeName =
            node.ParentNode?.SelectSingleNode(".//span[contains(@class, 'd-title')]")
                ?.InnerText.Trim()
            ?? string.Empty;
        var malId = node.GetAttributeValue("data-mal", string.Empty);
        var slug = node.GetAttributeValue("data-slug", string.Empty);
        var timestamp = node.GetAttributeValue("data-timestamp", string.Empty);
        var epUrl = $"{_episodeUrlSuffixRegex.Replace(animePath, string.Empty)}/ep-{numberString}";

        var id = new StringBuilder();
        id.Append(ids);
        id.Append("&epurl=");
        id.Append(epUrl);
        if (!string.IsNullOrWhiteSpace(malId))
            id.Append("&mal=").Append(malId);
        if (!string.IsNullOrWhiteSpace(slug))
            id.Append("&slug=").Append(slug);
        if (!string.IsNullOrWhiteSpace(timestamp))
            id.Append("&ts=").Append(timestamp);

        return new Episode
        {
            Id = id.ToString(),
            Number = number,
            Name =
                $"Episode {numberString}"
                + (
                    string.IsNullOrWhiteSpace(episodeName)
                    || episodeName == $"Episode {numberString}"
                        ? string.Empty
                        : $": {episodeName}"
                ),
            Description = string.Join(
                ", ",
                new[] { sub, softSub, dub }.Where(x => !string.IsNullOrWhiteSpace(x))
            ),
            Link = epUrl,
        };
    }

    private async ValueTask<VideoServer?> GetVideoServerAsync(
        HtmlNode node,
        string referer,
        CancellationToken cancellationToken
    )
    {
        var serverId = node.GetAttributeValue("data-link-id", string.Empty);
        if (string.IsNullOrWhiteSpace(serverId))
            return null;

        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/ajax/server?get={serverId}",
            BuildAjaxHeaders(referer),
            cancellationToken
        );
        var embed = JsonNode.Parse(response)?["result"]?["url"]?.ToString();
        if (string.IsNullOrWhiteSpace(embed))
            return null;

        var label = node.ParentNode?.ParentNode?.SelectSingleNode(".//label")?.InnerText.Trim();
        var name = node.InnerText.Trim();
        if (!string.IsNullOrWhiteSpace(label))
            name = $"{NormalizeTypeLabel(label!)} - {name}";

        return new VideoServer
        {
            Name = name,
            Embed = new FileUrl(NormalizeUrl(embed!, BaseUrl))
            {
                Headers = new Dictionary<string, string> { ["Referer"] = referer },
            },
        };
    }

    private static string? ExtractVideoUrl(JsonNode? sources)
    {
        if (sources is JsonObject sourceObject)
            return sourceObject["file"]?.ToString();

        if (sources is JsonArray sourceArray)
        {
            foreach (var source in sourceArray)
            {
                if (source is JsonObject obj)
                    return obj["file"]?.ToString();
                if (source is JsonValue)
                    return source.ToString();
            }
        }

        if (sources is JsonValue)
            return sources.ToString();

        return null;
    }

    private static List<Subtitle> ParsePlayerSubtitles(JsonArray? tracks)
    {
        if (tracks is null)
            return [];

        return tracks
            .OfType<JsonObject>()
            .Where(track =>
                string.Equals(
                    track["kind"]?.ToString(),
                    "captions",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .Select(track => new Subtitle(
                track["file"]?.ToString() ?? string.Empty,
                track["label"]?.ToString() ?? "Subtitle"
            ))
            .Where(track => !string.IsNullOrWhiteSpace(track.Url))
            .ToList();
    }

    private static string NormalizeAnimePath(string id)
    {
        var value = Before(id, "#");
        if (string.IsNullOrWhiteSpace(value))
            value = id;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            value = uri.AbsolutePath;

        value = Before(value, "?");
        value = _episodeUrlSuffixRegex.Replace(value, string.Empty);
        return value.StartsWith("/") ? value : "/" + value.Trim('/');
    }

    private static string ExtractFragment(string value) =>
        value.IndexOf('#') >= 0 ? After(value, "#") : string.Empty;

    private static string GetTitle(HtmlNode? node)
    {
        if (node is null)
            return string.Empty;

        var english = WebUtility.HtmlDecode(node.InnerText).Trim();
        var japanese = node.GetAttributeValue("data-jp", string.Empty).Trim();
        return DefaultIfBlank(english, japanese);
    }

    private static string? SelectMetaValue(HtmlDocument document, string label)
    {
        var node = document.DocumentNode.SelectSingleNode(
            $"//div[contains(@class, 'meta')]/div[contains(., '{label}')]/span"
        );
        return node?.InnerText.Trim();
    }

    private static string NormalizeTypeLabel(string label) =>
        label.ToUpperInvariant() switch
        {
            "SUB" => "Sub",
            "H-SUB" => "H-Sub",
            "DUB" => "Dub",
            "A-DUB" => "A-Dub",
            _ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(label.ToLowerInvariant()),
        };

    private static Dictionary<string, string> BuildHeaders(string referer) =>
        new() { ["Referer"] = referer.EndsWith("/") ? referer : referer + "/" };

    private static Dictionary<string, string> BuildAjaxHeaders(string referer) =>
        new()
        {
            ["Accept"] = "application/json, text/javascript, */*; q=0.01",
            ["Referer"] = referer,
            ["X-Requested-With"] = "XMLHttpRequest",
        };

    /// <summary>
    /// Encodes an Aniwave VRF token.
    /// </summary>
    public static string VrfEncrypt(string input)
    {
        var vrf = Exchange(input, "AP6GeR8H0lwUz1", "UAz8Gwl10P6ReH");
        vrf = Rc4Base64("ItFKjuWokn4ZpB", vrf);
        vrf = Rc4Base64("fOyt97QWFB3", vrf);
        vrf = Exchange(vrf, "1majSlPQd2M5", "da1l2jSmP5QM");
        vrf = Exchange(vrf, "CPYvHj09Au3", "0jHA9CPYu3v");
        vrf = new string(vrf.Reverse().ToArray());
        vrf = Rc4Base64("736y1uTJpBLUX", vrf);
        vrf = Base64UrlEncode(Encoding.UTF8.GetBytes(vrf));
        return Uri.EscapeDataString(vrf);
    }

    private static string Exchange(string input, string key1, string key2)
    {
        var chars = input.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var index = key1.IndexOf(chars[i]);
            if (index >= 0)
                chars[i] = key2[index];
        }

        return new string(chars);
    }

    private static string Rc4Base64(string key, string input) =>
        Base64UrlEncode(Rc4(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(input)));

    private static byte[] Rc4(byte[] key, byte[] input)
    {
        var s = Enumerable.Range(0, 256).Select(x => (byte)x).ToArray();
        var j = 0;
        for (var i = 0; i < 256; i++)
        {
            j = (j + s[i] + key[i % key.Length]) & 255;
            var temp = s[i];
            s[i] = s[j];
            s[j] = temp;
        }

        var output = new byte[input.Length];
        var a = 0;
        j = 0;
        for (var index = 0; index < input.Length; index++)
        {
            a = (a + 1) & 255;
            j = (j + s[a]) & 255;
            var temp = s[a];
            s[a] = s[j];
            s[j] = temp;
            output[index] = (byte)(input[index] ^ s[(s[a] + s[j]) & 255]);
        }

        return output;
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_');

    private static string NormalizeUrl(string url, string baseUrl)
    {
        if (url.StartsWith("//"))
            return "https:" + url;

        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            return url;

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            return url;

        return url.StartsWith("/")
            ? uri.GetLeftPart(UriPartial.Authority) + url
            : uri.GetLeftPart(UriPartial.Authority) + "/" + url;
    }

    private static string DefaultIfBlank(string? value, string? fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback ?? string.Empty : value!;

    private static bool ContainsIgnoreCase(string value, string needle) =>
        value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

    private static string Before(string? value, string marker)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var index = value.IndexOf(marker, StringComparison.Ordinal);
        return index < 0 ? value : value.Substring(0, index);
    }

    private static string After(string value, string marker)
    {
        var index = value.IndexOf(marker, StringComparison.Ordinal);
        return index < 0 ? string.Empty : value.Substring(index + marker.Length);
    }
}
