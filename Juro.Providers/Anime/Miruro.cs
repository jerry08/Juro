using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
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

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with Miruro.
/// </summary>
public class Miruro(IHttpClientFactory httpClientFactory)
    : AnimeBaseProvider(httpClientFactory),
        IAnimeProvider,
        IPopularProvider,
        ILastUpdatedProvider
{
    private const string PreferredProvider = "kiwi";
    private const string PreferredSubType = "sub";
    private static readonly byte[] _pipeKey = HexToBytes("71951034f8fbcf53d89db52ceb3dc22c");
    private static readonly Regex _htmlRegex = new("<[^>]+>", RegexOptions.Compiled);

    private readonly HttpClient _http = httpClientFactory.CreateClient();

    public string Key => Name;

    public string Name => "Miruro.tv";

    public string Language => "en";

    public bool IsDubAvailableSeparately => true;

    public string BaseUrl => "https://www.miruro.tv";

    /// <summary>
    /// Initializes an instance of <see cref="Miruro"/>.
    /// </summary>
    public Miruro(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="Miruro"/>.
    /// </summary>
    public Miruro()
        : this(Http.ClientProvider) { }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        var queryObject = BuildPipeQuery(
            ("q", query),
            ("type", "ANIME"),
            ("limit", 20),
            ("offset", 0)
        );
        var json = await SendPipeAsync("search", queryObject, cancellationToken);
        return ParseAnimeList(json, "results", "data");
    }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> GetPopularAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var query = BuildPipeQuery(
            ("type", "ANIME"),
            ("status", "RELEASING"),
            ("page", page),
            ("perPage", 20),
            ("sort", "TRENDING_DESC")
        );
        var json = await SendPipeAsync("search/browse", query, cancellationToken);
        return ParseAnimeList(json);
    }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> GetLastUpdatedAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var query = BuildPipeQuery(
            ("type", "ANIME"),
            ("status", "RELEASING"),
            ("page", page),
            ("perPage", 20),
            ("sort", "UPDATED_AT_DESC")
        );
        var json = await SendPipeAsync("search/browse", query, cancellationToken);
        return ParseAnimeList(json);
    }

    /// <inheritdoc />
    public async ValueTask<IAnimeInfo> GetAnimeInfoAsync(
        string animeId,
        CancellationToken cancellationToken = default
    )
    {
        var json = await SendPipeAsync($"info/{animeId}", new JsonObject(), cancellationToken);
        var root = JsonNode.Parse(json);
        var media = root?["media"] ?? root;
        if (media is not JsonObject mediaObject)
            return new AnimeInfo
            {
                Id = animeId,
                Site = AnimeSites.Miruro,
                Title = string.Empty,
            };

        var titleObject = mediaObject["title"] as JsonObject;
        var anime = new AnimeInfo
        {
            Id = GetString(mediaObject, "id", animeId),
            Site = AnimeSites.Miruro,
            Title = ResolveTitle(titleObject),
            Image =
                ExtractCoverImage(mediaObject["coverImage"])
                ?? ExtractString(mediaObject["bannerImage"]),
            Summary = StripHtml(GetString(mediaObject, "description")),
            Status = GetString(mediaObject, "status"),
            Category = ExtractMainStudio(mediaObject["studios"]),
            Link = $"{BaseUrl}/watch/{animeId}",
        };

        if (mediaObject["genres"] is JsonArray genres)
        {
            anime.Genres = genres
                .Select(x => ExtractString(x))
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
        var query = BuildPipeQuery(("anilistId", int.Parse(animeId)));
        var json = await SendPipeAsync("episodes", query, cancellationToken);
        var root = JsonNode.Parse(json) as JsonObject;
        var providers = root?["providers"] as JsonObject;
        if (providers is null)
            return [];

        var episodes = ParseEpisodesFromProvider(
            providers[PreferredProvider] as JsonObject,
            PreferredProvider
        );
        if (episodes.Count == 0)
        {
            foreach (var provider in providers)
            {
                if (provider.Key == "hop" || provider.Value is not JsonObject providerData)
                    continue;

                episodes = ParseEpisodesFromProvider(providerData, provider.Key);
                if (episodes.Count > 0)
                    break;
            }
        }

        return episodes.OrderByDescending(x => x.Number).ToList();
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    )
    {
        var episodeData = JsonNode.Parse(episodeId) as JsonObject;
        if (episodeData is null)
            return [];

        var provider = GetString(episodeData, "provider", PreferredProvider);
        var defaultSubType = GetString(episodeData, "defaultSubType", PreferredSubType);
        var subTypes = episodeData["subTypes"] as JsonObject;
        var servers = new List<VideoServer>();

        var defaultEpisodeId = GetString(episodeData, "episodeId");
        if (!string.IsNullOrWhiteSpace(defaultEpisodeId))
        {
            servers.AddRange(
                await GetStreamServersAsync(
                    defaultEpisodeId,
                    provider,
                    defaultSubType,
                    cancellationToken
                )
            );
        }

        if (subTypes is null || subTypes.Count <= 1)
            return servers;

        foreach (var subType in subTypes)
        {
            if (subType.Key == defaultSubType)
                continue;

            var subEpisodeId = ExtractString(subType.Value);
            if (string.IsNullOrWhiteSpace(subEpisodeId))
                continue;

            servers.AddRange(
                await GetStreamServersAsync(subEpisodeId!, provider, subType.Key, cancellationToken)
            );
        }

        return servers;
    }

    /// <inheritdoc />
    public override ValueTask<List<VideoSource>> GetVideosAsync(
        VideoServer server,
        CancellationToken cancellationToken = default
    )
    {
        if (!Uri.IsWellFormedUriString(server.Embed.Url, UriKind.Absolute))
            return new ValueTask<List<VideoSource>>([]);

        var isHls = server.Embed.Url.Contains(".m3u8", StringComparison.OrdinalIgnoreCase);
        return new ValueTask<List<VideoSource>>([
            new VideoSource
            {
                Title = server.Name,
                Resolution = server.Name,
                VideoUrl = server.Embed.Url,
                Headers = new Dictionary<string, string>(server.Embed.Headers),
                Format = isHls ? VideoType.M3u8 : VideoType.Container,
                FileType = isHls ? "m3u8" : "mp4",
                VideoServer = server,
            },
        ]);
    }

    private async ValueTask<List<VideoServer>> GetStreamServersAsync(
        string episodeId,
        string provider,
        string category,
        CancellationToken cancellationToken
    )
    {
        var query = BuildPipeQuery(
            ("episodeId", episodeId),
            ("provider", provider),
            ("category", category)
        );
        var json = await SendPipeAsync("sources", query, cancellationToken);
        var root = JsonNode.Parse(json) as JsonObject;
        var streams = root?["streams"] as JsonArray;
        if (streams is null)
            return [];

        var subTypeLabel = category switch
        {
            "sub" => "Sub",
            "dub" => "Dub",
            "ssub" => "Soft Sub",
            _ => category,
        };

        var servers = new List<VideoServer>();
        foreach (var streamNode in streams.OfType<JsonObject>())
        {
            if (!GetString(streamNode, "type").Equals("hls", StringComparison.OrdinalIgnoreCase))
                continue;

            var url = GetString(streamNode, "url");
            if (string.IsNullOrWhiteSpace(url))
                continue;

            var quality = GetString(streamNode, "quality");
            var codec = GetString(streamNode, "codec");
            var audio = GetString(streamNode, "audio");
            var fansub = GetString(streamNode, "fansub");
            var referer = GetString(streamNode, "referer", "https://kwik.cx/");
            var resolution = streamNode["resolution"] as JsonObject;
            var dimensions = resolution is null
                ? string.Empty
                : $" - {GetString(resolution, "width")}x{GetString(resolution, "height")}";

            var name = string.Join(
                " ",
                new[]
                {
                    string.IsNullOrWhiteSpace(quality) ? "Auto" : $"{quality}p",
                    subTypeLabel,
                    dimensions,
                    codec,
                    audio,
                    fansub,
                }.Where(x => !string.IsNullOrWhiteSpace(x))
            );

            servers.Add(
                new VideoServer
                {
                    Name = name,
                    Embed = new FileUrl(url)
                    {
                        Headers = new Dictionary<string, string> { ["Referer"] = referer },
                    },
                }
            );
        }

        return servers;
    }

    private List<Episode> ParseEpisodesFromProvider(JsonObject? providerData, string provider)
    {
        var episodesObject = providerData?["episodes"] as JsonObject;
        if (episodesObject is null)
            return [];

        var subTypes = provider == "bee" ? new[] { "ssub", "sub", "dub" } : new[] { "sub", "dub" };
        var episodeMap = new Dictionary<float, Dictionary<string, string>>();
        var titles = new Dictionary<float, string>();

        foreach (var subType in subTypes)
        {
            if (episodesObject[subType] is not JsonArray typedEpisodes)
                continue;

            foreach (var episodeNode in typedEpisodes.OfType<JsonObject>())
            {
                var number = GetFloat(episodeNode, "number");
                var id = GetString(episodeNode, "id");
                if (number <= 0 || string.IsNullOrWhiteSpace(id))
                    continue;

                if (!episodeMap.TryGetValue(number, out var subTypeIds))
                {
                    subTypeIds = new Dictionary<string, string>();
                    episodeMap[number] = subTypeIds;
                }

                subTypeIds[subType] = id;
                if (!titles.ContainsKey(number))
                    titles[number] = GetString(episodeNode, "title");
            }
        }

        return episodeMap
            .Select(pair =>
            {
                titles.TryGetValue(pair.Key, out var title);
                return BuildEpisode(pair.Key, title, provider, pair.Value);
            })
            .Where(x => x is not null)
            .Cast<Episode>()
            .ToList();
    }

    private Episode? BuildEpisode(
        float number,
        string? title,
        string provider,
        Dictionary<string, string> subTypeIds
    )
    {
        var defaultSubType = subTypeIds.ContainsKey(PreferredSubType)
            ? PreferredSubType
            : subTypeIds.Keys.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(defaultSubType))
            return null;

        var subTypesObject = new JsonObject();
        foreach (var subTypeId in subTypeIds)
            subTypesObject[subTypeId.Key] = subTypeId.Value;

        var episodeIdObject = new JsonObject
        {
            ["episodeId"] = subTypeIds[defaultSubType!],
            ["provider"] = provider,
            ["defaultSubType"] = defaultSubType,
            ["subTypes"] = subTypesObject,
        };

        return new Episode
        {
            Id = episodeIdObject.ToJsonString(),
            Number = number,
            Name = string.IsNullOrWhiteSpace(title)
                ? $"Episode {FormatEpisodeNumber(number)}"
                : $"Episode {FormatEpisodeNumber(number)}: {title}",
        };
    }

    private async ValueTask<string> SendPipeAsync(
        string path,
        JsonObject query,
        CancellationToken cancellationToken
    )
    {
        var payload = new JsonObject
        {
            ["path"] = path,
            ["method"] = "GET",
            ["query"] = query,
            ["body"] = null,
            ["version"] = "0.2.0",
        };
        var encoded = Base64UrlEncode(Encoding.UTF8.GetBytes(payload.ToJsonString()));
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{BaseUrl}/api/secure/pipe?e={encoded}"
        );

#if NETCOREAPP
        // Miruro's edge hard-403s HTTP/1.1 pipe requests; browsers use h2.
        request.Version = HttpVersion.Version20;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
#endif

        // Miruro's Cloudflare edge enforces a WAF rule that 403s pipe-API
        // requests whose headers don't match a real Chrome CORS fetch.
        foreach (var header in Http.ApiFingerprintHeaders(BaseUrl))
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);

        using var response = await _http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var obfuscated = response.Headers.TryGetValues("x-obfuscated", out var values)
            ? values.FirstOrDefault()
            : "1";

        return obfuscated == "2" ? DecryptPipeResponse(body) : body.Trim();
    }

    private List<IAnimeInfo> ParseAnimeList(string json, params string[] fallbackKeys)
    {
        var root = JsonNode.Parse(json);
        var mediaArray = root as JsonArray;
        if (mediaArray is null && root is JsonObject rootObject)
        {
            mediaArray = rootObject["media"] as JsonArray;
            foreach (var fallbackKey in fallbackKeys)
            {
                mediaArray ??= rootObject[fallbackKey] as JsonArray;
            }
        }

        if (mediaArray is null)
            return [];

        return mediaArray
            .OfType<JsonObject>()
            .Select(ParseAnimeFromMedia)
            .Where(x => x is not null)
            .Cast<IAnimeInfo>()
            .ToList();
    }

    private AnimeInfo? ParseAnimeFromMedia(JsonObject media)
    {
        var id = GetString(media, "id");
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return new AnimeInfo
        {
            Id = id,
            Site = AnimeSites.Miruro,
            Title = ResolveTitle(media["title"] as JsonObject),
            Image = ExtractCoverImage(media["coverImage"]) ?? ExtractString(media["bannerImage"]),
            Link = $"{BaseUrl}/watch/{id}",
        };
    }

    private static JsonObject BuildPipeQuery(params (string Key, object? Value)[] pairs)
    {
        var query = new JsonObject();
        foreach (var (key, value) in pairs)
        {
            if (value is null)
                continue;

            query[key] = value switch
            {
                int intValue => intValue,
                long longValue => longValue,
                double doubleValue => doubleValue,
                float floatValue => floatValue,
                bool boolValue => boolValue,
                _ => value.ToString(),
            };
        }

        return query;
    }

    private static string DecryptPipeResponse(string body)
    {
        var decoded = Base64UrlDecode(body.Trim());
        for (var i = 0; i < decoded.Length; i++)
            decoded[i] = (byte)(decoded[i] ^ _pipeKey[i % _pipeKey.Length]);

        using var input = new MemoryStream(decoded);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string ResolveTitle(JsonObject? titleObject)
    {
        if (titleObject is null)
            return string.Empty;

        return GetString(titleObject, "userPreferred")
            .DefaultIfBlank(GetString(titleObject, "romaji"))
            .DefaultIfBlank(GetString(titleObject, "english"))
            .DefaultIfBlank(GetString(titleObject, "native"));
    }

    private static string? ExtractCoverImage(JsonNode? coverImage) =>
        coverImage switch
        {
            JsonObject obj => GetString(obj, "extraLarge")
                .DefaultIfBlank(GetString(obj, "large"))
                .DefaultIfBlank(GetString(obj, "medium")),
            _ => ExtractString(coverImage),
        };

    private static string ExtractMainStudio(JsonNode? studios)
    {
        var edges = studios switch
        {
            JsonObject obj => obj["edges"] as JsonArray,
            JsonArray array => array,
            _ => null,
        };
        if (edges is null)
            return string.Empty;

        foreach (var edge in edges.OfType<JsonObject>())
        {
            if (GetBool(edge, "isMain"))
                return GetString(edge["node"] as JsonObject, "name");
        }

        return GetString(
            edges.OfType<JsonObject>().FirstOrDefault()?["node"] as JsonObject,
            "name"
        );
    }

    private static string StripHtml(string html) =>
        _htmlRegex
            .Replace(Regex.Replace(html, "<br>", "\n", RegexOptions.IgnoreCase), string.Empty)
            .Trim();

    private static string? ExtractString(JsonNode? node) =>
        node is null ? null : node.ToString().Trim('"');

    private static string GetString(JsonObject? obj, string key, string defaultValue = "") =>
        obj is null ? defaultValue : ExtractString(obj[key]) ?? defaultValue;

    private static float GetFloat(JsonObject obj, string key) =>
        float.TryParse(GetString(obj, key), out var value) ? value : 0;

    private static bool GetBool(JsonObject obj, string key) =>
        bool.TryParse(GetString(obj, key), out var value) && value;

    private static string FormatEpisodeNumber(float number) =>
        number % 1 == 0 ? $"{number:0}" : $"{number:0.0}";

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }

        return Convert.FromBase64String(base64);
    }

    private static byte[] HexToBytes(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }
}

file static class MiruroStringExtensions
{
    public static string DefaultIfBlank(this string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value!;
}
