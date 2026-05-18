using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core;
using Juro.Core.Models;
using Juro.Core.Models.Anime;
using Juro.Core.Models.Anime.AnimePahe;
using Juro.Core.Models.Videos;
using Juro.Core.Providers;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;
using Juro.Core.Utils.Tasks;
using Juro.Extractors;

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with AnimePahe.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="AnimePahe"/>.
/// </remarks>
public class AnimePahe(IHttpClientFactory httpClientFactory)
    : AnimeBaseProvider(httpClientFactory),
        IAnimeProvider
{
    private const string DesktopUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36";

    private static readonly Regex _animeSessionRegex = new(
        @"/anime/(?<session>[\w-]+)",
        RegexOptions.Compiled
    );
    private static readonly Regex _animeIdRegex = new(
        @"(?:/a/|anime_id=)(?<id>\d+)",
        RegexOptions.Compiled
    );
    private static readonly Regex _videoServerRegex = new(
        @"(?<group>.+?)\s*·\s*(?<quality>\d+)p\s*\((?<size>.+?)MB\)\s*(?<audio>.*)",
        RegexOptions.Compiled
    );

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public string Key => Name;

    public string Name => "AnimePahe";

    public string Language => "en";

    public bool IsDubAvailableSeparately => false;

    public string BaseUrl => "https://animepahe.pw";

    /// <summary>
    /// Initializes an instance of <see cref="AnimePahe"/>.
    /// </summary>
    public AnimePahe(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="AnimePahe"/>.
    /// </summary>
    public AnimePahe()
        : this(Http.ClientProvider) { }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    ) => await SearchAsync(query, true, cancellationToken);

    /// <inheritdoc cref="IAnimeProvider.SearchAsync" />
    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        bool useId,
        CancellationToken cancellationToken = default
    )
    {
        var http = CreateClient();
        var response = await http.ExecuteAsync(
            $"{BaseUrl}/api?m=search&q={Uri.EscapeDataString(query)}",
            BuildHeaders(),
            cancellationToken
        );
        var data = JsonNode.Parse(response)?["data"] as JsonArray;
        if (data is null)
            return [];

        return data.OfType<JsonObject>()
            .Select(x =>
                (IAnimeInfo)
                    new AnimePaheInfo
                    {
                        Id = useId ? GetString(x, "id") : GetString(x, "session"),
                        Title = GetString(x, "title"),
                        Type = GetString(x, "type"),
                        Episodes = GetInt(x, "episodes"),
                        Status = GetString(x, "status"),
                        Season = GetString(x, "season"),
                        Released = GetString(x, "year"),
                        Score = GetFloat(x, "score"),
                        Image = GetString(x, "poster"),
                        Site = AnimeSites.AnimePahe,
                        Link = BuildAnimeLink(useId ? GetString(x, "id") : GetString(x, "session")),
                    }
            )
            .ToList();
    }

    /// <inheritdoc cref="IPopularProvider.GetPopularAsync"/>
    public async ValueTask<List<IAnimeInfo>> GetAiringAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var http = CreateClient();
        var response = await http.ExecuteAsync(
            $"{BaseUrl}/api?m=airing&page={page}",
            BuildHeaders(),
            cancellationToken
        );
        var data = JsonNode.Parse(response)?["data"] as JsonArray;
        if (data is null)
            return [];

        return data.OfType<JsonObject>()
            .Select(x =>
                (IAnimeInfo)
                    new AnimeInfo
                    {
                        Id = GetString(x, "anime_id"),
                        Title = GetString(x, "anime_title"),
                        Image = GetString(x, "snapshot"),
                        Site = AnimeSites.AnimePahe,
                        Link = BuildAnimeLink(GetString(x, "anime_id")),
                    }
            )
            .ToList();
    }

    /// <inheritdoc />
    public async ValueTask<IAnimeInfo> GetAnimeInfoAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var http = CreateClient();
        var path = BuildAnimePath(id);
        var response = await http.ExecuteAsync(
            $"{BaseUrl}{path}",
            BuildHeaders(),
            cancellationToken
        );
        var document = Html.Parse(response);

        var anime = new AnimePaheInfo { Id = NormalizeAnimeId(id), Site = AnimeSites.AnimePahe };

        anime.Title =
            document
                .DocumentNode.SelectSingleNode("//div[contains(@class, 'title-wrapper')]/h1/span")
                ?.InnerText.Trim()
            ?? document
                .DocumentNode.SelectSingleNode(
                    ".//div[contains(@class, 'header-wrapper')]/header/div/h1/span"
                )
                ?.InnerText.Trim()
            ?? string.Empty;

        anime.Image =
            document
                .DocumentNode.SelectSingleNode("//div[contains(@class, 'anime-poster')]//a")
                ?.GetAttributeValue("href", string.Empty)
            ?? document
                .DocumentNode.SelectSingleNode(".//header/div/div/div/a/img")
                ?.GetAttributeValue("data-src", string.Empty);

        anime.Summary =
            document
                .DocumentNode.SelectSingleNode("//div[contains(@class, 'anime-summary')]")
                ?.InnerText.Trim()
            ?? string.Empty;

        anime.Genres =
            document
                .DocumentNode.SelectNodes(
                    "//div[contains(@class, 'anime-genre')]//li | //div[contains(@class, 'anime-info')]//p[contains(., 'Demographic:')]/a | //div[contains(@class, 'anime-info')]//p[contains(., 'Theme:')]/a"
                )
                ?.Select(el => new Genre(el.InnerText.Trim()))
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .ToList()
            ?? [];

        anime.Status = SelectInfoValue(document, "Status");
        anime.Category = SelectInfoValue(document, "Studios");
        anime.Season = SelectInfoValue(document, "Season") ?? string.Empty;
        anime.Released = SelectInfoValue(document, "Aired")
            ?.Split(new[] { "to" }, StringSplitOptions.None)[0]
            .Trim();
        anime.OtherNames = DefaultIfBlank(
            SelectInfoValue(document, "Synonyms"),
            SelectInfoValue(document, "Japanese")
        );
        anime.Link = $"{BaseUrl}{path}";

        return anime;
    }

    /// <inheritdoc />
    public async ValueTask<List<Episode>> GetEpisodesAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var session = await ResolveSessionAsync(id, cancellationToken);
        if (string.IsNullOrWhiteSpace(session))
            return [];

        var http = CreateClient();
        var response = await http.ExecuteAsync(
            $"{BaseUrl}/api?m=release&id={session}&sort=episode_asc&page=1",
            BuildHeaders(),
            cancellationToken
        );
        var firstPage = JsonNode.Parse(response) as JsonObject;
        if (firstPage is null)
            return [];

        var episodes = ParseEpisodePage(firstPage, session).ToList();
        var lastPage = GetInt(firstPage, "last_page", 1);
        if (lastPage > 1)
        {
            var functions = Enumerable
                .Range(2, lastPage - 1)
                .Select(page =>
                    (Func<Task<string>>)(
                        async () =>
                            await http.ExecuteAsync(
                                $"{BaseUrl}/api?m=release&id={session}&sort=episode_asc&page={page}",
                                BuildHeaders(),
                                cancellationToken
                            )
                    )
                );

            var results = await TaskEx.Run(functions, 20);
            episodes.AddRange(
                results.SelectMany(result =>
                    ParseEpisodePage(JsonNode.Parse(result) as JsonObject ?? [], session)
                )
            );
        }

        return episodes.OrderBy(x => x.Number).ToList();
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    )
    {
        var http = CreateClient();
        var episodeUrl = Uri.IsWellFormedUriString(episodeId, UriKind.Absolute)
            ? episodeId
            : $"{BaseUrl}{episodeId}";
        var response = await http.ExecuteAsync(
            episodeUrl,
            new Dictionary<string, string> { ["Referer"] = $"{BaseUrl}/" },
            cancellationToken
        );
        var document = Html.Parse(response);
        var hlsButtons = document.DocumentNode.SelectNodes(
            "//div[@id='resolutionMenu']/button[@data-src]"
        );
        if (hlsButtons is not null)
        {
            var hlsServers = hlsButtons
                .Select(ParseHlsVideoServer)
                .Where(x => x is not null)
                .Cast<VideoServer>()
                .ToList();
            if (hlsServers.Count > 0)
                return hlsServers;
        }

        var downloadLinks = document.DocumentNode.SelectNodes("//div[@id='pickDownload']/a");
        if (downloadLinks is null)
            return [];

        return downloadLinks
            .Select(ParseVideoServer)
            .Where(x => x is not null)
            .Cast<VideoServer>()
            .ToList();
    }

    private VideoServer? ParseHlsVideoServer(HtmlAgilityPack.HtmlNode element)
    {
        var src = element.GetAttributeValue("data-src", string.Empty);
        if (string.IsNullOrWhiteSpace(src))
            return null;

        var fansub = element.GetAttributeValue("data-fansub", string.Empty);
        var resolution = element.GetAttributeValue("data-resolution", string.Empty);
        var audio = element.GetAttributeValue("data-audio", string.Empty);
        var av1 =
            element.GetAttributeValue("data-av1", string.Empty) == "1" ? " AV1" : string.Empty;
        var name = string.Join(
            " ",
            new[]
            {
                DefaultIfBlank(fansub, "AnimePahe"),
                string.IsNullOrWhiteSpace(resolution) ? string.Empty : $"- {resolution}p",
                string.IsNullOrWhiteSpace(audio)
                    ? string.Empty
                    : $"({audio.ToUpperInvariant()}{av1})",
                "HLS",
            }.Where(x => !string.IsNullOrWhiteSpace(x))
        );

        return new VideoServer
        {
            Name = name,
            Embed = new FileUrl(src)
            {
                Headers = new Dictionary<string, string> { ["Referer"] = $"{BaseUrl}/" },
            },
        };
    }

    /// <inheritdoc />
    public override async ValueTask<List<VideoSource>> GetVideosAsync(
        VideoServer server,
        CancellationToken cancellationToken = default
    )
    {
        if (!Uri.IsWellFormedUriString(server.Embed.Url, UriKind.Absolute))
            return [];

        var videos = await new KwikExtractor(_httpClientFactory).ExtractAsync(
            server.Embed.Url,
            server.Embed.Headers,
            cancellationToken
        );

        videos.ForEach(x => x.VideoServer = server);
        return videos;
    }

    private VideoServer? ParseVideoServer(HtmlAgilityPack.HtmlNode element)
    {
        var href = element.GetAttributeValue("href", string.Empty);
        if (string.IsNullOrWhiteSpace(href))
            return null;

        var text = element.InnerText.Trim();
        var match = _videoServerRegex.Match(text);
        var group = DefaultIfBlank(match.Groups["group"].Value, text);
        var quality = match.Groups["quality"].Value;
        var audio = match.Groups["audio"].Value.Trim();
        var audioName = string.IsNullOrWhiteSpace(audio) ? string.Empty : $" {audio}";
        var name = string.IsNullOrWhiteSpace(quality) ? group : $"{group}{audioName} - {quality}p";

        return new VideoServer
        {
            Name = name,
            Embed = new FileUrl(href)
            {
                Headers = new Dictionary<string, string> { ["Referer"] = $"{BaseUrl}/" },
            },
        };
    }

    private async ValueTask<string> ResolveSessionAsync(
        string id,
        CancellationToken cancellationToken
    )
    {
        var session = ExtractSession(id);
        if (!string.IsNullOrWhiteSpace(session))
            return session!;

        var animeId = ExtractAnimeId(id);
        if (string.IsNullOrWhiteSpace(animeId))
            return id.Trim('/');

        return await FetchSessionAsync(animeId!, cancellationToken);
    }

    private async ValueTask<string> FetchSessionAsync(
        string animeId,
        CancellationToken cancellationToken
    )
    {
        var http = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/a/{animeId}");
        foreach (var header in BuildHeaders())
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);

        using var response = await http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        var redirectedPath = response.RequestMessage?.RequestUri?.AbsolutePath;
        var session = ExtractSession(redirectedPath);
        if (!string.IsNullOrWhiteSpace(session))
            return session!;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return ExtractSession(body) ?? string.Empty;
    }

    private static IEnumerable<Episode> ParseEpisodePage(JsonObject page, string animeSession)
    {
        if (page["data"] is not JsonArray episodes)
            yield break;

        foreach (var episode in episodes.OfType<JsonObject>())
        {
            var session = GetString(episode, "session");
            if (string.IsNullOrWhiteSpace(session))
                continue;

            var number = GetFloat(episode, "episode");
            var name =
                number % 1 == 0
                    ? $"Episode {number:0}"
                    : $"Episode {number.ToString(CultureInfo.InvariantCulture)}";
            var link = $"/play/{animeSession}/{session}";

            yield return new Episode
            {
                Id = link,
                Name = name,
                Number = number,
                Image = GetString(episode, "snapshot"),
                Description = GetString(episode, "title"),
                Link = link,
                Duration = ParseDuration(GetString(episode, "duration")),
            };
        }
    }

    private HttpClient CreateClient() => _httpClientFactory.CreateClient().BypassDdg();

    private Dictionary<string, string> BuildHeaders() =>
        new() { ["Referer"] = $"{BaseUrl}/", ["User-Agent"] = DesktopUserAgent };

    private string BuildAnimePath(string id)
    {
        var session = ExtractSession(id);
        if (!string.IsNullOrWhiteSpace(session))
            return $"/anime/{session}";

        var animeId = ExtractAnimeId(id);
        if (!string.IsNullOrWhiteSpace(animeId))
            return $"/a/{animeId}";

        return id.StartsWith("/") ? id : $"/anime/{id.Trim('/')}";
    }

    private string BuildAnimeLink(string id) => $"{BaseUrl}{BuildAnimePath(id)}";

    private string NormalizeAnimeId(string id) => ExtractAnimeId(id) ?? ExtractSession(id) ?? id;

    private static string? ExtractSession(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return NullIfBlank(_animeSessionRegex.Match(value).Groups["session"].Value);
    }

    private static string? ExtractAnimeId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (int.TryParse(value.Trim('/'), out _))
            return value.Trim('/');

        return NullIfBlank(_animeIdRegex.Match(value).Groups["id"].Value);
    }

    private static string? SelectInfoValue(HtmlAgilityPack.HtmlDocument document, string label)
    {
        var node = document.DocumentNode.SelectSingleNode(
            $"//div[contains(@class, 'anime-info')]//p[contains(., '{label}:')]"
        );
        var linkText = node?.SelectSingleNode(".//a")?.InnerText.Trim();
        var text = node?.InnerText.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return linkText;

        var prefix = label + ":";
        if (text.StartsWith(prefix))
            text = text.Substring(prefix.Length).Trim();

        return DefaultIfBlank(linkText, text);
    }

    private static string GetString(JsonObject? obj, string key, string defaultValue = "") =>
        obj?[key]?.ToString().Trim('"') ?? defaultValue;

    private static int GetInt(JsonObject obj, string key, int defaultValue = 0) =>
        int.TryParse(
            GetString(obj, key),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var value
        )
            ? value
            : defaultValue;

    private static float GetFloat(JsonObject obj, string key) =>
        float.TryParse(
            GetString(obj, key),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var value
        )
            ? value
            : 0;

    private static float ParseDuration(string value) =>
        TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var duration)
            ? (float)duration.TotalMilliseconds
            : 0;

    private static string DefaultIfBlank(string? value, string? fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback ?? string.Empty : value!;

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
