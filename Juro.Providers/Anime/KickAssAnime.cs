using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core;
using Juro.Core.Models;
using Juro.Core.Models.Anime;
using Juro.Core.Models.Videos;
using Juro.Core.Providers;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with KickAssAnime.
/// </summary>
public class KickAssAnime(IHttpClientFactory httpClientFactory)
    : AnimeBaseProvider(httpClientFactory),
        IAnimeProvider,
        IPopularProvider,
        ILastUpdatedProvider
{
    private const string SearchBaseUrl = "https://kaa.lt";
    private const string PreferredLanguage = "ja-JP";
    private const string SecondaryLanguage = "en-US";
    private const string MobileUserAgent =
        "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Mobile Safari/537.36";

    private static readonly Regex _httpSlashRegex = new(@"^(https?:)//+", RegexOptions.Compiled);
    private static readonly Regex _manifestRegex = new(
        "\"manifest\":\\[0,\"(?:https?:)?(?<url>//[^\"]+)\"",
        RegexOptions.Compiled
    );
    private static readonly Regex _trackRegex = new(
        "\"language\":\\[\\d+,\"(?<language>[^\"]+)\"\\][^}]+?\"name\":\\[\\d+,\"(?<name>[^\"]+)\"\\][^}]+?\"src\":\\[\\d+,\"(?<src>[^\"]+)\"\\]",
        RegexOptions.Compiled
    );
    private static readonly Regex _cidRegex = new(
        @"cid:\s*'(?<value>[^']+)'",
        RegexOptions.Compiled
    );
    private static readonly Regex _encryptedPayloadRegex = new(
        @":""(?<value>[^""]+)""",
        RegexOptions.Compiled
    );

    private readonly HttpClient _http = httpClientFactory.CreateClient();

    private string ApiUrl => $"{BaseUrl}/api/show";

    public string Key => Name;

    public string Name => "KickAssAnime";

    public string Language => "en";

    public bool IsDubAvailableSeparately => true;

    public string BaseUrl => SearchBaseUrl;

    /// <summary>
    /// Initializes an instance of <see cref="KickAssAnime"/>.
    /// </summary>
    public KickAssAnime(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="KickAssAnime"/>.
    /// </summary>
    public KickAssAnime()
        : this(Http.ClientProvider) { }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        var payload = new JsonObject { ["page"] = 1, ["query"] = query };
        var response = await PostJsonAsync(
            $"{SearchBaseUrl}/api/fsearch",
            payload,
            $"{SearchBaseUrl}/search?q={Uri.EscapeDataString(query)}",
            cancellationToken
        );

        return ParseAnimeList(response);
    }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> GetPopularAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{ApiUrl}/trending?page={page}",
            BuildJsonHeaders(BaseUrl),
            cancellationToken
        );

        return ParseAnimeList(response);
    }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> GetLastUpdatedAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{ApiUrl}/recent?type=all&page={page}",
            BuildJsonHeaders(BaseUrl),
            cancellationToken
        );

        return ParseAnimeList(response);
    }

    /// <inheritdoc />
    public async ValueTask<IAnimeInfo> GetAnimeInfoAsync(
        string animeId,
        CancellationToken cancellationToken = default
    )
    {
        var slug = NormalizeShowSlug(animeId);
        var response = await _http.ExecuteAsync(
            $"{ApiUrl}/{slug}",
            BuildJsonHeaders($"{BaseUrl}/{slug}"),
            cancellationToken
        );
        var media = JsonNode.Parse(response) as JsonObject;
        if (media is null)
            return new AnimeInfo
            {
                Id = slug,
                Site = AnimeSites.KickAssAnime,
                Title = string.Empty,
            };

        var title = ResolveTitle(media);
        var anime = new AnimeInfo
        {
            Id = slug,
            Site = AnimeSites.KickAssAnime,
            Title = title,
            Image = GetPosterUrl(media),
            Summary = GetString(media, "synopsis"),
            Status = ParseStatus(GetString(media, "status")),
            Category = FormatTitleCase(GetString(media, "season")),
            Released = GetString(media, "year"),
            OtherNames =
                GetString(media, "title_en") == title ? null : GetString(media, "title_en"),
            Link = $"{BaseUrl}/{slug}",
        };

        if (media["genres"] is JsonArray genres)
        {
            anime.Genres = genres
                .Select(ExtractString)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => new Genre(x!))
                .ToList();
        }

        return anime;
    }

    /// <inheritdoc />
    public async ValueTask<List<Episode>> GetEpisodesAsync(
        string animeId,
        CancellationToken cancellationToken = default
    )
    {
        var slug = NormalizeShowSlug(animeId);
        var language = await ResolveLanguageAsync(slug, cancellationToken);
        var firstPage = await GetEpisodePageAsync(slug, 1, language, cancellationToken);
        var episodes = ParseEpisodePage(firstPage, slug).ToList();
        var pageCount = GetPageCount(firstPage);

        for (var page = 2; page <= pageCount; page++)
        {
            var pageJson = await GetEpisodePageAsync(slug, page, language, cancellationToken);
            episodes.AddRange(ParseEpisodePage(pageJson, slug));
        }

        return episodes.OrderBy(x => x.Number).ToList();
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    )
    {
        var episodePath = NormalizeEpisodePath(episodeId);
        var apiPath = episodePath.Replace("/ep-", "/episode/ep-");
        var response = await _http.ExecuteAsync(
            $"{ApiUrl}{apiPath}",
            BuildJsonHeaders($"{BaseUrl}{episodePath}"),
            cancellationToken
        );
        var servers = JsonNode.Parse(response)?["servers"] as JsonArray;
        if (servers is null)
            return [];

        return servers
            .OfType<JsonObject>()
            .Select(server =>
            {
                var src = FixUrl(GetString(server, "src"), BaseUrl);
                var name = GetString(server, "name", "KickAssAnime");
                return string.IsNullOrWhiteSpace(src)
                    ? null
                    : new VideoServer
                    {
                        Name = name,
                        Embed = new FileUrl(src!)
                        {
                            Headers = BuildPageHeaders($"{BaseUrl}{episodePath}"),
                        },
                    };
            })
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
        if (!Uri.IsWellFormedUriString(server.Embed.Url, UriKind.Absolute))
            return [];

        var embedUrl = ContainsIgnoreCase(server.Embed.Url, "/vast")
            ? ReplacePath(server.Embed.Url, "/cat-player/player")
            : server.Embed.Url;

        var response = await _http.ExecuteAsync(
            embedUrl,
            server.Embed.Headers.Count == 0
                ? BuildPageHeaders(BaseUrl)
                : new Dictionary<string, string>(server.Embed.Headers),
            cancellationToken
        );
        var html = WebUtility.HtmlDecode(response).Replace("&quot;", "\"");

        var videos = ParseNewPlayer(html, embedUrl, server);
        if (videos.Count > 0)
            return videos;

        return await ParseLegacyPlayerAsync(html, embedUrl, server, cancellationToken);
    }

    private async ValueTask<string> PostJsonAsync(
        string url,
        JsonObject payload,
        string referer,
        CancellationToken cancellationToken
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        foreach (var header in BuildJsonHeaders(referer))
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return await _http.ExecuteAsync(request, cancellationToken);
    }

    private async ValueTask<string> ResolveLanguageAsync(
        string slug,
        CancellationToken cancellationToken
    )
    {
        var response = await _http.ExecuteAsync(
            $"{ApiUrl}/{slug}/language",
            BuildJsonHeaders($"{BaseUrl}/{slug}"),
            cancellationToken
        );
        var languages = JsonNode.Parse(response)?["result"] as JsonArray;
        var values = languages
            ?.Select(ExtractString)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        if (values is null || values.Count == 0)
            return PreferredLanguage;

        return values.FirstOrDefault(x => x == PreferredLanguage)
            ?? values.FirstOrDefault(x => x == SecondaryLanguage)
            ?? values[0]!;
    }

    private async ValueTask<JsonObject> GetEpisodePageAsync(
        string slug,
        int page,
        string language,
        CancellationToken cancellationToken
    )
    {
        var response = await _http.ExecuteAsync(
            $"{ApiUrl}/{slug}/episodes?page={page}&lang={Uri.EscapeDataString(language)}",
            BuildJsonHeaders($"{BaseUrl}/{slug}"),
            cancellationToken
        );

        return JsonNode.Parse(response) as JsonObject ?? [];
    }

    private static IEnumerable<Episode> ParseEpisodePage(JsonObject page, string showSlug)
    {
        if (page["result"] is not JsonArray episodes)
            yield break;

        foreach (var episode in episodes.OfType<JsonObject>())
        {
            var episodeString = GetString(episode, "episode_string");
            var episodeSlug = GetString(episode, "slug");
            if (string.IsNullOrWhiteSpace(episodeString) || string.IsNullOrWhiteSpace(episodeSlug))
                continue;

            var number = TryGetFloat(episodeString, out var parsed) ? parsed : 0;
            var title = GetString(episode, "title");
            var name = $"Ep. {episodeString}";
            if (!string.IsNullOrWhiteSpace(title))
                name += $" - {title}";

            var path = $"/{showSlug}/ep-{episodeString}-{episodeSlug}";
            yield return new Episode
            {
                Id = path,
                Number = number,
                Name = name,
                Description = title,
                Link = path,
            };
        }
    }

    private static int GetPageCount(JsonObject page)
    {
        if (page["pages"] is JsonArray pages && pages.Count > 0)
            return pages.Count;

        return GetInt(page, "page_count", 1);
    }

    private static List<VideoSource> ParseNewPlayer(
        string html,
        string embedUrl,
        VideoServer server
    )
    {
        var match = _manifestRegex.Match(html.Replace("\\/", "/"));
        if (!match.Success)
            return [];

        var manifestUrl = FixUrl(match.Groups["url"].Value, embedUrl);
        if (string.IsNullOrWhiteSpace(manifestUrl))
            return [];

        var headers = BuildVideoHeaders(embedUrl);
        var subtitles = ParseSubtitles(html, embedUrl, headers);
        return [BuildVideoSource(server, manifestUrl!, server.Name, headers, subtitles)];
    }

    private async ValueTask<List<VideoSource>> ParseLegacyPlayerAsync(
        string html,
        string embedUrl,
        VideoServer server,
        CancellationToken cancellationToken
    )
    {
        if (!_cidRegex.IsMatch(html) || !Uri.TryCreate(embedUrl, UriKind.Absolute, out var uri))
            return [];

        var queryName = server.Name.Equals("DuckStream", StringComparison.OrdinalIgnoreCase)
            ? "mid"
            : "id";
        var query = GetQueryParameter(uri, queryName);
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var key = GetLegacyKey(server.Name);
        if (key.Length == 0)
            return [];

        var signature = GetSignature(html, server.Name, query!, key);
        if (signature is null)
            return [];

        var sourceUrl =
            $"{uri.Scheme}://{uri.Host}{signature.Value.Route}?{queryName}={Uri.EscapeDataString(query!)}";
        if (!server.Name.Equals("BirdStream", StringComparison.OrdinalIgnoreCase))
            sourceUrl += $"&e={signature.Value.Timestamp}";
        sourceUrl += $"&s={signature.Value.Hash}";

        var response = await _http.ExecuteAsync(
            sourceUrl,
            new Dictionary<string, string>
            {
                ["Accept"] = "*/*",
                ["Referer"] = embedUrl,
                ["Origin"] = uri.GetLeftPart(UriPartial.Authority),
            },
            cancellationToken
        );

        var encrypted = _encryptedPayloadRegex.Match(response).Groups["value"].Value;
        var parts = encrypted.Replace("\\", string.Empty).Split(':');
        if (parts.Length < 2)
            return [];

        var decrypted = DecryptLegacyPayload(parts[0], key, HexToBytes(parts[1]));
        if (string.IsNullOrWhiteSpace(decrypted))
            return [];

        var videoObject = JsonNode.Parse(decrypted) as JsonObject;
        if (videoObject is null)
            return [];

        var playlistUrl = FixUrl(
            DefaultIfBlank(GetString(videoObject, "hls"), GetString(videoObject, "dash")),
            embedUrl
        );
        if (string.IsNullOrWhiteSpace(playlistUrl))
            return [];

        var headers = BuildVideoHeaders(embedUrl);
        var subtitles = ParseJsonSubtitles(
            videoObject["subtitles"] as JsonArray,
            embedUrl,
            headers
        );
        return [BuildVideoSource(server, playlistUrl!, server.Name, headers, subtitles)];
    }

    private static VideoSource BuildVideoSource(
        VideoServer server,
        string url,
        string title,
        Dictionary<string, string> headers,
        List<Subtitle> subtitles
    )
    {
        var isDash = ContainsIgnoreCase(url, ".mpd");
        var isHls = ContainsIgnoreCase(url, ".m3u8");
        return new VideoSource
        {
            Title = title,
            Resolution = isHls ? "Multi Quality" : title,
            VideoUrl = url,
            Format =
                isDash ? VideoType.Dash
                : isHls ? VideoType.M3u8
                : VideoType.Container,
            FileType =
                isDash ? "mpd"
                : isHls ? "m3u8"
                : "mp4",
            Headers = headers,
            Subtitles = subtitles,
            VideoServer = server,
        };
    }

    private static List<Subtitle> ParseSubtitles(
        string html,
        string embedUrl,
        Dictionary<string, string> headers
    ) =>
        _trackRegex
            .Matches(html.Replace("\\/", "/"))
            .OfType<Match>()
            .Select(match =>
            {
                var src = FixUrl(match.Groups["src"].Value, embedUrl);
                var name = match.Groups["name"].Value;
                var language = match.Groups["language"].Value;
                return string.IsNullOrWhiteSpace(src)
                    ? null
                    : new Subtitle(
                        src!,
                        $"{name} ({language})",
                        new Dictionary<string, string>(headers)
                    );
            })
            .Where(x => x is not null)
            .Cast<Subtitle>()
            .ToList();

    private static List<Subtitle> ParseJsonSubtitles(
        JsonArray? subtitles,
        string embedUrl,
        Dictionary<string, string> headers
    )
    {
        if (subtitles is null)
            return [];

        return subtitles
            .OfType<JsonObject>()
            .Select(subtitle =>
            {
                var src = FixUrl(GetString(subtitle, "src"), embedUrl);
                if (string.IsNullOrWhiteSpace(src))
                    return null;

                var name = GetString(subtitle, "name", "Subtitle");
                var language = GetString(subtitle, "language");
                return new Subtitle(
                    src!,
                    string.IsNullOrWhiteSpace(language) ? name : $"{name} ({language})",
                    new Dictionary<string, string>(headers)
                );
            })
            .Where(x => x is not null)
            .Cast<Subtitle>()
            .ToList();
    }

    private static List<IAnimeInfo> ParseAnimeList(string json)
    {
        var root = JsonNode.Parse(json);
        var items = root as JsonArray ?? root?["result"] as JsonArray;
        if (items is null)
            return [];

        return items
            .OfType<JsonObject>()
            .Select(ParseAnimeItem)
            .Where(x => x is not null)
            .Cast<IAnimeInfo>()
            .ToList();
    }

    private static AnimeInfo? ParseAnimeItem(JsonObject item)
    {
        var slug = GetString(item, "slug").Trim('/');
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        return new AnimeInfo
        {
            Id = slug,
            Site = AnimeSites.KickAssAnime,
            Title = ResolveTitle(item),
            Image = GetPosterUrl(item),
            Link = $"{SearchBaseUrl}/{slug}",
        };
    }

    private static string ResolveTitle(JsonObject item) =>
        DefaultIfBlank(GetString(item, "title"), GetString(item, "title_en"));

    private static string? GetPosterUrl(JsonObject item)
    {
        var poster = item["poster"] as JsonObject;
        var slug = GetString(poster, "hq");
        if (string.IsNullOrWhiteSpace(slug))
            slug = ExtractString(item["poster"]);

        return string.IsNullOrWhiteSpace(slug) ? null : $"{SearchBaseUrl}/image/poster/{slug}.webp";
    }

    private static string NormalizeShowSlug(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            value = uri.AbsolutePath;

        value = value.Trim('/');
        if (value.StartsWith("api/show/"))
            value = value.Substring("api/show/".Length);

        return value.Split('/')[0];
    }

    private static string NormalizeEpisodePath(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            value = uri.AbsolutePath;

        value = value.Trim();
        return value.StartsWith("/") ? value : "/" + value.Trim('/');
    }

    private static string? FixUrl(string? rawUrl, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            return null;

        var trimmed = WebUtility.HtmlDecode(rawUrl).Replace("\\/", "/").Trim();
        if (trimmed.StartsWith("http://") || trimmed.StartsWith("https://"))
            return _httpSlashRegex.Replace(trimmed, "$1//");

        if (trimmed.StartsWith("//"))
            return "https://" + trimmed.TrimStart('/');

        if (trimmed.StartsWith("/") && Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            return uri.GetLeftPart(UriPartial.Authority) + trimmed;

        return trimmed;
    }

    private static string ReplacePath(string url, string path)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        var builder = new UriBuilder(uri) { Path = path, Query = uri.Query.TrimStart('?') };
        return builder.Uri.ToString();
    }

    private static Dictionary<string, string> BuildJsonHeaders(string referer) =>
        new()
        {
            ["Accept"] = "application/json, text/plain, */*",
            ["Referer"] = referer,
            ["Origin"] = SearchBaseUrl,
        };

    private static Dictionary<string, string> BuildPageHeaders(string referer) =>
        new()
        {
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            ["Referer"] = referer,
        };

    private static Dictionary<string, string> BuildVideoHeaders(string url)
    {
        var origin = Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.GetLeftPart(UriPartial.Authority)
            : SearchBaseUrl;

        return new Dictionary<string, string>
        {
            ["Accept"] = "*/*",
            ["Accept-Language"] = "en-US,en;q=0.9",
            ["Origin"] = origin,
            ["Sec-Fetch-Dest"] = "empty",
            ["Sec-Fetch-Mode"] = "cors",
            ["Sec-Fetch-Site"] = "same-site",
            ["User-Agent"] = MobileUserAgent,
        };
    }

    private static (string Hash, string Timestamp, string Route)? GetSignature(
        string html,
        string server,
        string query,
        byte[] key
    )
    {
        var cidMatch = _cidRegex.Match(html);
        if (!cidMatch.Success)
            return null;

        var cid = Encoding.UTF8.GetString(HexToBytes(cidMatch.Groups["value"].Value)).Split('|');
        if (cid.Length < 2)
            return null;

        var timestamp = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60).ToString(
            CultureInfo.InvariantCulture
        );
        var route = cid[1].Replace("player.php", "source.php");
        var order = server switch
        {
            "VidStreaming" or "DuckStream" => new[]
            {
                "IP",
                "USERAGENT",
                "ROUTE",
                "MID",
                "TIMESTAMP",
                "KEY",
            },
            "BirdStream" => new[] { "IP", "USERAGENT", "ROUTE", "MID", "KEY" },
            _ => [],
        };
        if (order.Length == 0)
            return null;

        var builder = new StringBuilder();
        foreach (var item in order)
        {
            switch (item)
            {
                case "IP":
                    builder.Append(cid[0]);
                    break;
                case "USERAGENT":
                    builder.Append(MobileUserAgent);
                    break;
                case "ROUTE":
                    builder.Append(route);
                    break;
                case "MID":
                    builder.Append(query);
                    break;
                case "TIMESTAMP":
                    builder.Append(timestamp);
                    break;
                case "KEY":
                    builder.Append(Encoding.UTF8.GetString(key));
                    break;
            }
        }

        return (Sha1(builder.ToString()), timestamp, route);
    }

    private static byte[] GetLegacyKey(string server) =>
        server switch
        {
            "VidStreaming" => Encoding.UTF8.GetBytes("e13d38099bf562e8b9851a652d2043d3"),
            "DuckStream" => Encoding.UTF8.GetBytes("4504447b74641ad972980a6b8ffd7631"),
            "BirdStream" => Encoding.UTF8.GetBytes("4b14d0ff625163e3c9c7a47926484bf2"),
            _ => [],
        };

    private static string DecryptLegacyPayload(string encryptedData, byte[] key, byte[] iv)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var encryptedBytes = Convert.FromBase64String(encryptedData);
            var decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string Sha1(string value)
    {
        using var sha1 = SHA1.Create();
        return string.Concat(
            sha1.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(x => x.ToString("x2"))
        );
    }

    private static string? GetQueryParameter(Uri uri, string key)
    {
        var query = uri.Query.TrimStart('?').Split('&');
        foreach (var part in query)
        {
            var values = part.Split(new[] { '=' }, 2);
            if (values.Length == 2 && values[0] == key)
                return WebUtility.UrlDecode(values[1]);
        }

        return null;
    }

    private static byte[] HexToBytes(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    private static string ParseStatus(string status) =>
        status switch
        {
            "finished_airing" => "Completed",
            "currently_airing" => "Ongoing",
            _ => status,
        };

    private static string FormatTitleCase(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? value
            : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.Replace('_', ' '));

    private static string? ExtractString(JsonNode? node) =>
        node is null ? null : node.ToString().Trim('"');

    private static string GetString(JsonObject? obj, string key, string defaultValue = "") =>
        obj is null ? defaultValue : ExtractString(obj[key]) ?? defaultValue;

    private static int GetInt(JsonObject obj, string key, int defaultValue) =>
        int.TryParse(
            GetString(obj, key),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var value
        )
            ? value
            : defaultValue;

    private static bool TryGetFloat(string value, out float result) =>
        float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

    private static string DefaultIfBlank(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value!;

    private static bool ContainsIgnoreCase(string value, string needle) =>
        value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
}
