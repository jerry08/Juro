using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
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
using Juro.Extractors;
using Juro.Providers.Anime.Zoro;

namespace Juro.Providers.Anime.Zoro;

/// <summary>
/// Abstract base class for ZoroTheme-based anime providers.
/// </summary>
public abstract class ZoroTheme
    : AnimeBaseProvider,
        IAnimeProvider,
        IPopularProvider,
        ILastUpdatedProvider
{
    private readonly HttpClient _http;
    private readonly IHttpClientFactory _httpClientFactory;

    public abstract string Key { get; }
    public abstract string Name { get; }
    public abstract string Language { get; }
    public abstract string BaseUrl { get; }

    public virtual bool IsDubAvailableSeparately => false;

    /// <summary>
    /// List of supported hoster names for this provider.
    /// </summary>
    protected abstract List<string> HosterNames { get; }

    /// <summary>
    /// Optional ajax route prefix (e.g., "/v2" for some sites).
    /// </summary>
    protected virtual string AjaxRoute => "";

    /// <summary>
    /// Whether to use English titles instead of Romaji.
    /// </summary>
    public bool UseEnglishTitles { get; set; } = false;

    /// <summary>
    /// Whether to mark filler episodes.
    /// </summary>
    public bool MarkFillerEpisodes { get; set; } = true;

    /// <summary>
    /// Preferred video quality (e.g., "1080", "720").
    /// </summary>
    public string PreferredQuality { get; set; } = "1080";

    /// <summary>
    /// Preferred video type (e.g., "Sub", "Dub").
    /// </summary>
    public string PreferredType { get; set; } = "Sub";

    /// <summary>
    /// Preferred server name.
    /// </summary>
    public string? PreferredServer { get; set; }

    /// <summary>
    /// Enabled hosters for video extraction.
    /// </summary>
    public HashSet<string> EnabledHosters { get; set; } = [];

    /// <summary>
    /// Enabled video types (servers-sub, servers-dub, servers-mixed, servers-raw).
    /// </summary>
    public HashSet<string> EnabledTypes { get; set; } =
        ["servers-sub", "servers-dub", "servers-mixed", "servers-raw"];

    protected ZoroTheme(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _http = httpClientFactory.CreateClient();
    }

    protected ZoroTheme(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    protected ZoroTheme()
        : this(Http.ClientProvider) { }

    #region Request Headers

    protected virtual Dictionary<string, string> GetDocHeaders()
    {
        var uri = new Uri(BaseUrl);
        return new Dictionary<string, string>
        {
            ["Accept"] =
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8",
            ["Host"] = uri.Host,
            ["Referer"] = $"{BaseUrl}/",
        };
    }

    protected virtual Dictionary<string, string> GetApiHeaders(string referer)
    {
        var uri = new Uri(BaseUrl);
        return new Dictionary<string, string>
        {
            ["Accept"] = "*/*",
            ["Host"] = uri.Host,
            ["Referer"] = referer,
            ["X-Requested-With"] = "XMLHttpRequest",
        };
    }

    #endregion

    #region Popular Anime

    /// <inheritdoc />
    public virtual async ValueTask<List<IAnimeInfo>> GetPopularAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var url = $"{BaseUrl}/most-popular?page={page}";
        var response = await _http.ExecuteAsync(url, GetDocHeaders(), cancellationToken);
        return ParseAnimeList(response);
    }

    #endregion

    #region Latest Updates

    /// <inheritdoc />
    public virtual async ValueTask<List<IAnimeInfo>> GetLastUpdatedAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var url = $"{BaseUrl}/top-airing?page={page}";
        var response = await _http.ExecuteAsync(url, GetDocHeaders(), cancellationToken);
        return ParseAnimeList(response);
    }

    #endregion

    #region Search

    /// <inheritdoc />
    public virtual async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        return await SearchAsync(query, new ZoroThemeSearchParameters(), cancellationToken);
    }

    /// <summary>
    /// Searches for anime with advanced filter parameters.
    /// </summary>
    public virtual async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        ZoroThemeSearchParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        var endpoint = string.IsNullOrWhiteSpace(query) ? "filter" : "search";
        var url = BuildSearchUrl(endpoint, query, parameters);

        var response = await _http.ExecuteAsync(url, GetDocHeaders(), cancellationToken);
        return ParseAnimeList(response);
    }

    private string BuildSearchUrl(string endpoint, string query, ZoroThemeSearchParameters p)
    {
        var queryParams = new List<string> { $"page={p.Page}" };

        AddIfNotBlank(queryParams, "keyword", query);
        AddIfNotBlank(queryParams, "type", p.Type);
        AddIfNotBlank(queryParams, "status", p.Status);
        AddIfNotBlank(queryParams, "rated", p.Rated);
        AddIfNotBlank(queryParams, "score", p.Score);
        AddIfNotBlank(queryParams, "season", p.Season);
        AddIfNotBlank(queryParams, "language", p.Language);
        AddIfNotBlank(queryParams, "sort", p.Sort);
        AddIfNotBlank(queryParams, "sy", p.StartYear);
        AddIfNotBlank(queryParams, "sm", p.StartMonth);
        AddIfNotBlank(queryParams, "sd", p.StartDay);
        AddIfNotBlank(queryParams, "ey", p.EndYear);
        AddIfNotBlank(queryParams, "em", p.EndMonth);
        AddIfNotBlank(queryParams, "ed", p.EndDay);
        AddIfNotBlank(queryParams, "genres", p.Genres);

        return $"{BaseUrl}/{endpoint}?{string.Join("&", queryParams)}";
    }

    private static void AddIfNotBlank(List<string> queryParams, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            queryParams.Add($"{key}={Uri.EscapeDataString(value)}");
        }
    }

    #endregion

    #region Anime Parsing

    private List<IAnimeInfo> ParseAnimeList(string? response)
    {
        var list = new List<IAnimeInfo>();

        if (string.IsNullOrWhiteSpace(response))
            return list;

        var document = Html.Parse(response!);
        var nodes = document.DocumentNode.SelectNodes("//div[contains(@class, 'flw-item')]");

        if (nodes is null)
            return list;

        foreach (var node in nodes)
        {
            var anime = ParseAnimeFromElement(node);
            if (anime is not null)
                list.Add(anime);
        }

        return list;
    }

    private IAnimeInfo? ParseAnimeFromElement(HtmlNode node)
    {
        var linkNode = node.SelectSingleNode(".//div[contains(@class, 'film-detail')]//a");
        if (linkNode is null)
            return null;

        var href = linkNode.GetAttributeValue("href", "");
        var title =
            UseEnglishTitles && linkNode.Attributes["title"] is not null
                ? linkNode.GetAttributeValue("title", "")
                : linkNode.GetAttributeValue("data-jname", "");

        var imgNode = node.SelectSingleNode(".//div[contains(@class, 'film-poster')]//img");
        var image = imgNode?.GetAttributeValue("data-src", "");

        return new AnimeInfo
        {
            Id = href,
            Site = GetAnimeSite(),
            Title = title,
            Image = image,
            Link = $"{BaseUrl}{href}",
        };
    }

    /// <summary>
    /// Gets the AnimeSites enum value for this provider.
    /// </summary>
    protected abstract AnimeSites GetAnimeSite();

    #endregion

    #region Anime Details

    /// <inheritdoc />
    public virtual async ValueTask<IAnimeInfo> GetAnimeInfoAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var url = id.StartsWith("http") ? id : $"{BaseUrl}{id}";
        var response = await _http.ExecuteAsync(url, GetDocHeaders(), cancellationToken);

        var document = Html.Parse(response);

        var anime = new AnimeInfo
        {
            Id = id,
            Site = GetAnimeSite(),
            Link = url,
        };

        // Thumbnail
        var posterImg = document.DocumentNode.SelectSingleNode(
            "//div[contains(@class, 'anisc-poster')]//img"
        );
        anime.Image = posterImg?.GetAttributeValue("src", "");

        // Info section
        var infoNode = document.DocumentNode.SelectSingleNode(
            "//div[contains(@class, 'anisc-info')]"
        );
        if (infoNode is not null)
        {
            anime.Title = GetInfoText(infoNode, "h2") ?? GetInfoText(infoNode, "h1") ?? "";

            anime.Summary = GetInfoValue(infoNode, "Overview:");
            anime.Status = GetInfoValue(infoNode, "Status:");
            anime.Released = GetInfoValue(infoNode, "Aired:");
            anime.Type = GetInfoValue(infoNode, "Type:");
            anime.OtherNames =
                GetInfoValue(infoNode, "Synonyms:") ?? GetInfoValue(infoNode, "Japanese:");

            // Studios as Category
            anime.Category = GetInfoValue(infoNode, "Studios:");

            // Genres
            var genreNodes = infoNode.SelectNodes(
                ".//div[contains(@class, 'item-list')][contains(., 'Genres:')]//a"
            );
            if (genreNodes is not null)
            {
                anime.Genres = genreNodes.Select(g => new Genre(g.InnerText.Trim())).ToList();
            }
        }

        return anime;
    }

    private static string? GetInfoText(HtmlNode infoNode, string tag)
    {
        return infoNode.SelectSingleNode($".//{tag}")?.InnerText?.Trim();
    }

    private static string? GetInfoValue(HtmlNode infoNode, string label)
    {
        var itemNode = infoNode.SelectSingleNode(
            $".//div[contains(@class, 'item-title')][contains(., '{label}')]"
        );

        if (itemNode is null)
            return null;

        var valueNode = itemNode.SelectSingleNode(
            ".//*[contains(@class, 'name') or contains(@class, 'text')]"
        );
        return valueNode?.InnerText?.Trim();
    }

    #endregion

    #region Episodes

    /// <inheritdoc />
    public virtual async ValueTask<List<Episode>> GetEpisodesAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        // First, we need to get the data-id from the anime page
        var animePageUrl = id.StartsWith("http") ? id : $"{BaseUrl}{id}";
        var animePageResponse = await _http.ExecuteAsync(
            animePageUrl,
            GetDocHeaders(),
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(animePageResponse))
            return [];

        var animeDocument = Html.Parse(animePageResponse);

        // Extract data-id attribute from the page (similar to how Aniwave does it)
        var dataIdNode = animeDocument.DocumentNode.SelectSingleNode("//*[@data-id]");
        var dataId = dataIdNode?.GetAttributeValue("data-id", "");

        if (string.IsNullOrWhiteSpace(dataId))
        {
            // Fallback: try to extract from the URL slug
            dataId = ExtractAnimeSlugFromUrl(id);
        }

        var url = $"{BaseUrl}/ajax{AjaxRoute}/episode/list/{dataId}";
        var response = await _http.ExecuteAsync(
            url,
            GetApiHeaders(animePageUrl),
            cancellationToken
        );

        var html = ParseHtmlFromJson(response);
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var document = Html.Parse(html);
        var nodes = document.DocumentNode.SelectNodes("//a[contains(@class, 'ep-item')]");

        if (nodes is null)
            return [];

        var episodes = new List<Episode>();

        foreach (var node in nodes)
        {
            var episodeNumber = float.TryParse(
                node.GetAttributeValue("data-number", "1"),
                out var num
            )
                ? num
                : 1f;

            var episodeTitle = node.GetAttributeValue("title", "");
            var episodeHref = node.GetAttributeValue("href", "");
            var isFiller = node.GetAttributeValue("class", "").Contains("ssl-item-filler");

            var episode = new Episode
            {
                Id = episodeHref,
                Number = episodeNumber,
                Name = $"Ep. {episodeNumber}: {episodeTitle}",
                Link = $"{BaseUrl}{episodeHref}",
            };

            if (isFiller && MarkFillerEpisodes)
            {
                episode.Description = "Filler Episode";
            }

            episodes.Add(episode);
        }

        // Return in ascending order (original is reversed)
        episodes.Reverse();
        return episodes;
    }

    /// <summary>
    /// Extracts the anime slug from a URL (e.g., "steinsgate-3" from "/watch/steinsgate-3").
    /// </summary>
    protected static string ExtractAnimeSlugFromUrl(string url)
    {
        var path = url.Split('?')[0];

        // Remove leading slashes and "watch/" prefix if present
        path = path.TrimStart('/');
        if (path.StartsWith("watch/"))
            path = path.Substring(6);

        // Return the slug (last path segment)
        var lastSlashIndex = path.LastIndexOf('/');
        return lastSlashIndex >= 0 ? path.Substring(lastSlashIndex + 1) : path;
    }

    /// <summary>
    /// Extracts the numeric ID from the end of a URL path.
    /// </summary>
    protected static string ExtractNumericIdFromUrl(string url)
    {
        var path = url.Split('?')[0];
        var lastDashIndex = path.LastIndexOf('-');
        return lastDashIndex >= 0 ? path.Substring(lastDashIndex + 1) : path;
    }

    #endregion

    #region Video Servers

    /// <inheritdoc />
    public virtual async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    )
    {
        // Episode ID format is like "steinsgate-3?ep=213"
        var epId = episodeId.Contains("?ep=")
            ? episodeId.Split(new[] { "?ep=" }, StringSplitOptions.None).Last()
            : ExtractNumericIdFromUrl(episodeId);

        var referer = episodeId.StartsWith("http") ? episodeId : $"{BaseUrl}{episodeId}";

        var url = $"{BaseUrl}/ajax{AjaxRoute}/episode/servers?episodeId={epId}";
        var response = await _http.ExecuteAsync(url, GetApiHeaders(referer), cancellationToken);

        var html = ParseHtmlFromJson(response);
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var document = Html.Parse(html);
        var servers = new List<VideoServer>();

        var serverTypes = new[] { "servers-sub", "servers-dub", "servers-mixed", "servers-raw" };

        foreach (var serverType in serverTypes)
        {
            if (!EnabledTypes.Contains(serverType))
                continue;

            var typeLabel = serverType.Replace("servers-", "").ToUpperInvariant();
            var serverNodes = document.DocumentNode.SelectNodes(
                $"//div[contains(@class, '{serverType}')]//div[contains(@class, 'item')]"
            );

            if (serverNodes is null)
                continue;

            foreach (var serverNode in serverNodes)
            {
                var serverId = serverNode.GetAttributeValue("data-id", "");
                var serverDataType = serverNode.GetAttributeValue("data-type", "");
                var serverName = serverNode.InnerText.Trim();

                if (!IsHosterEnabled(serverName))
                    continue;

                var embedUrl = await GetServerEmbedUrlAsync(serverId, referer, cancellationToken);
                if (string.IsNullOrWhiteSpace(embedUrl))
                    continue;

                servers.Add(
                    new VideoServer
                    {
                        Name = $"{serverName} ({typeLabel})",
                        Embed = new FileUrl(embedUrl)
                        {
                            Headers = new Dictionary<string, string> { ["Referer"] = BaseUrl },
                        },
                    }
                );
            }
        }

        return servers;
    }

    private async ValueTask<string?> GetServerEmbedUrlAsync(
        string serverId,
        string referer,
        CancellationToken cancellationToken
    )
    {
        var url = $"{BaseUrl}/ajax{AjaxRoute}/episode/sources?id={serverId}";
        var response = await _http.ExecuteAsync(url, GetApiHeaders(referer), cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
            return null;

        try
        {
            var json = JsonNode.Parse(response);
            return json?["link"]?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private bool IsHosterEnabled(string hosterName)
    {
        if (EnabledHosters.Count == 0)
        {
            // If no specific hosters are enabled, use all available hosters
#if NETCOREAPP
            EnabledHosters = HosterNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
#else
            EnabledHosters = new HashSet<string>(HosterNames, StringComparer.OrdinalIgnoreCase);
#endif
        }

        return EnabledHosters.Any(h => h.Equals(hosterName, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Video Extraction

    /// <summary>
    /// Override this method to provide custom video extractors for specific servers.
    /// </summary>
    public override IVideoExtractor? GetVideoExtractor(VideoServer server)
    {
        // Default implementation - subclasses should override for custom extractors
        return base.GetVideoExtractor(server);
    }

    /// <summary>
    /// Sorts videos based on user preferences.
    /// </summary>
    public virtual List<VideoSource> SortVideos(List<VideoSource> videos)
    {
        return videos
            .OrderByDescending(v => v.Resolution?.Contains(PreferredQuality) == true)
            .ThenByDescending(v =>
                v.VideoServer?.Name.Contains(
                    PreferredServer ?? "",
                    StringComparison.OrdinalIgnoreCase
                ) == true
            )
            .ThenByDescending(v =>
                v.VideoServer?.Name.Contains(PreferredType, StringComparison.OrdinalIgnoreCase)
                == true
            )
            .ToList();
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Parses the HTML content from a JSON response with an "html" field.
    /// </summary>
    protected static string? ParseHtmlFromJson(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        try
        {
            var json = JsonNode.Parse(response);
            return json?["html"]?.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses anime status string to standardized status.
    /// </summary>
    protected static string ParseStatus(string? statusString)
    {
        return statusString switch
        {
            "Currently Airing" => "Ongoing",
            "Finished Airing" => "Completed",
            _ => "Unknown",
        };
    }

    #endregion
}
