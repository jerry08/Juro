using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
using Juro.Extractors;

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with AniKai.to (AnimeKai).
/// This is a standalone provider — not ZoroTheme-based.
/// </summary>
public class AniKai : AnimeBaseProvider, IAnimeProvider
{
    private readonly HttpClient _http;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MegaUpExtractor _megaUp;

    public string Key => Name;
    public string Name => "AniKai";
    public string Language => "en";
    public string BaseUrl => "https://anikai.to";
    public bool IsDubAvailableSeparately => false;

    /// <summary>
    /// Initializes an instance of <see cref="AniKai"/>.
    /// </summary>
    public AniKai(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _http = httpClientFactory.CreateClient();
        _megaUp = new MegaUpExtractor(httpClientFactory);
    }

    /// <summary>
    /// Initializes an instance of <see cref="AniKai"/>.
    /// </summary>
    public AniKai(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="AniKai"/>.
    /// </summary>
    public AniKai()
        : this(Http.ClientProvider) { }

    #region Headers

    private Dictionary<string, string> GetHeaders()
    {
        var uri = new Uri(BaseUrl);
        return new Dictionary<string, string>
        {
            ["Accept"] = "text/html, */*; q=0.01",
            ["Accept-Language"] = "en-US,en;q=0.5",
            ["Host"] = uri.Host,
            ["Referer"] = $"{BaseUrl}/",
            ["Sec-Fetch-Dest"] = "empty",
            ["Sec-Fetch-Mode"] = "cors",
            ["Sec-Fetch-Site"] = "same-origin",
        };
    }

    private Dictionary<string, string> GetAjaxHeaders(string referer)
    {
        var headers = GetHeaders();
        headers["X-Requested-With"] = "XMLHttpRequest";
        headers["Referer"] = referer;
        return headers;
    }

    #endregion

    #region Search

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        var keyword = Regex.Replace(query, @"[\W_]+", "+");
        var url = $"{BaseUrl}/browser?keyword={keyword}";
        var response = await _http.ExecuteAsync(url, GetHeaders(), cancellationToken);
        return ParseAnimeList(response);
    }

    private List<IAnimeInfo> ParseAnimeList(string? response)
    {
        var list = new List<IAnimeInfo>();
        if (string.IsNullOrWhiteSpace(response))
            return list;

        var document = Html.Parse(response!);
        var nodes = document.DocumentNode.SelectNodes("//div[contains(@class, 'aitem')]");
        if (nodes is null)
            return list;

        foreach (var node in nodes)
        {
            var anime = ParseAnimeCard(node);
            if (anime is not null)
                list.Add(anime);
        }

        return list;
    }

    private IAnimeInfo? ParseAnimeCard(HtmlNode node)
    {
        var posterLink = node.SelectSingleNode(".//a[contains(@class, 'poster')]");
        if (posterLink is null)
            return null;

        var href = posterLink.GetAttributeValue("href", "");
        if (string.IsNullOrWhiteSpace(href))
            return null;

        var id = href.Replace("/watch/", "");

        var titleNode = node.SelectSingleNode(".//a[contains(@class, 'title')]");
        var title = titleNode?.GetAttributeValue("title", "") ?? titleNode?.InnerText?.Trim() ?? "";
        var japaneseTitle = titleNode?.GetAttributeValue("data-jp", "");

        var imgNode = node.SelectSingleNode(".//img");
        var image =
            imgNode?.GetAttributeValue("data-src", "") ?? imgNode?.GetAttributeValue("src", "");

        var infoNode = node.SelectSingleNode(".//div[contains(@class, 'info')]");
        var type = infoNode?.SelectNodes(".//span/b")?.LastOrDefault()?.InnerText?.Trim();

        return new AnimeInfo
        {
            Id = id,
            Site = AnimeSites.AniKai,
            Title = title,
            OtherNames = japaneseTitle,
            Image = image,
            Type = type,
            Link = $"{BaseUrl}{href}",
        };
    }

    #endregion

    #region Anime Info

    /// <inheritdoc />
    public async ValueTask<IAnimeInfo> GetAnimeInfoAsync(
        string animeId,
        CancellationToken cancellationToken = default
    )
    {
        var slug = animeId.Split('$')[0];
        var url = slug.StartsWith("http") ? slug : $"{BaseUrl}/watch/{slug}";
        var response = await _http.ExecuteAsync(url, GetHeaders(), cancellationToken);
        var document = Html.Parse(response);

        var anime = new AnimeInfo
        {
            Id = slug,
            Site = AnimeSites.AniKai,
            Link = url,
        };

        // Title
        var titleNode = document.DocumentNode.SelectSingleNode(
            "//div[contains(@class, 'entity-scroll')]//h1[contains(@class, 'title')]"
        );
        anime.Title = titleNode?.InnerText?.Trim() ?? "";
        anime.OtherNames = titleNode?.GetAttributeValue("data-jp", "");

        // Image
        var imgNode = document.DocumentNode.SelectSingleNode(
            "//div[contains(@class, 'poster')]//img"
        );
        anime.Image = imgNode?.GetAttributeValue("src", "");

        // Description
        var descNode = document.DocumentNode.SelectSingleNode(
            "//div[contains(@class, 'entity-scroll')]//div[contains(@class, 'desc')]"
        );
        anime.Summary = descNode?.InnerText?.Trim();

        // Type from info section
        var infoNode = document.DocumentNode.SelectSingleNode(
            "//div[contains(@class, 'entity-scroll')]//div[contains(@class, 'info')]"
        );
        if (infoNode is not null)
        {
            var lastBold = infoNode.SelectNodes(".//span/b")?.LastOrDefault();
            anime.Type = lastBold?.InnerText?.Trim();
        }

        // Detail section - parse label:value pairs
        var detailDivs = document.DocumentNode.SelectNodes(
            "//div[contains(@class, 'detail')]//div/div"
        );
        if (detailDivs is not null)
        {
            foreach (var div in detailDivs)
            {
                var text = div.InnerText?.Trim() ?? "";
                var spanNode = div.SelectSingleNode(".//span");
                var value = spanNode?.InnerText?.Trim() ?? "";

                if (text.StartsWith("Status:"))
                    anime.Status = value;
                else if (text.StartsWith("Date aired:"))
                    anime.Released = value;
                else if (text.StartsWith("Studios:"))
                    anime.Category = value;
                else if (text.StartsWith("Genres:"))
                {
                    var genreLinks = div.SelectNodes(".//a");
                    if (genreLinks is not null)
                    {
                        anime.Genres = genreLinks
                            .Select(g => new Genre(g.InnerText.Trim()))
                            .ToList();
                    }
                }
            }
        }

        return anime;
    }

    #endregion

    #region Episodes

    /// <inheritdoc />
    public async ValueTask<List<Episode>> GetEpisodesAsync(
        string animeId,
        CancellationToken cancellationToken = default
    )
    {
        var slug = animeId.Split('$')[0];
        var pageUrl = $"{BaseUrl}/watch/{slug}";

        // Fetch the watch page to get the anime rating data-id (ani_id)
        var pageResponse = await _http.ExecuteAsync(pageUrl, GetHeaders(), cancellationToken);
        if (string.IsNullOrWhiteSpace(pageResponse))
            return [];

        var document = Html.Parse(pageResponse);

        // Extract ani_id from the rate-box element
        var rateBox = document.DocumentNode.SelectSingleNode("//*[@id='anime-rating'][@data-id]");
        var aniId = rateBox?.GetAttributeValue("data-id", "");

        // Fallback: try to extract from the JSON data block in the page
        if (string.IsNullOrWhiteSpace(aniId))
        {
            var match = Regex.Match(pageResponse, @"""anime_id""\s*:\s*""([^""]+)""");
            if (match.Success)
                aniId = match.Groups[1].Value;
        }

        if (string.IsNullOrWhiteSpace(aniId))
            return [];

        // Generate token and fetch episode list
        var token = await _megaUp.GenerateTokenAsync(aniId!, cancellationToken);
        var ajaxUrl = $"{BaseUrl}/ajax/episodes/list?ani_id={aniId}&_={token}";
        var ajaxResponse = await _http.ExecuteAsync(
            ajaxUrl,
            GetAjaxHeaders(pageUrl),
            cancellationToken
        );

        var html = ParseResultHtml(ajaxResponse);
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var epDocument = Html.Parse(html);
        var epNodes = epDocument.DocumentNode.SelectNodes("//div[contains(@class, 'eplist')]//a");
        if (epNodes is null)
            return [];

        var episodes = new List<Episode>();
        foreach (var epNode in epNodes)
        {
            var num = epNode.GetAttributeValue("num", "1");
            var epToken = epNode.GetAttributeValue("token", "");
            var epTitle = epNode.SelectSingleNode(".//span")?.InnerText?.Trim() ?? "";
            var isFiller = epNode.GetAttributeValue("class", "").Contains("filler");

            var episodeNumber = float.TryParse(num, out var n) ? n : 1f;

            var episodeId = $"{slug}$ep={num}$token={epToken}";

            var episode = new Episode
            {
                Id = episodeId,
                Number = episodeNumber,
                Name = string.IsNullOrWhiteSpace(epTitle)
                    ? $"Episode {episodeNumber}"
                    : $"Ep. {episodeNumber}: {epTitle}",
                Link = $"{BaseUrl}/watch/{slug}?ep={num}",
            };

            if (isFiller)
                episode.Description = "Filler Episode";

            episodes.Add(episode);
        }

        return episodes;
    }

    #endregion

    #region Video Servers

    /// <inheritdoc />
    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    )
    {
        // Episode ID format: "slug$ep=1$token=xyz"
        var epToken = "";
        var parts = episodeId.Split(new[] { "$token=" }, StringSplitOptions.None);
        if (parts.Length >= 2)
            epToken = parts[1];

        if (string.IsNullOrWhiteSpace(epToken))
            return [];

        // Fetch server list
        var token = await _megaUp.GenerateTokenAsync(epToken, cancellationToken);
        var ajaxUrl = $"{BaseUrl}/ajax/links/list?token={epToken}&_={token}";
        var referer = $"{BaseUrl}/watch/{episodeId.Split('$')[0]}";
        var response = await _http.ExecuteAsync(
            ajaxUrl,
            GetAjaxHeaders(referer),
            cancellationToken
        );

        var html = ParseResultHtml(response);
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var document = Html.Parse(html);
        var servers = new List<VideoServer>();

        // Parse server groups: softsub, dub, raw
        var serverGroups = document.DocumentNode.SelectNodes(
            "//div[contains(@class, 'server-items')]"
        );
        if (serverGroups is null)
            return [];

        foreach (var group in serverGroups)
        {
            var groupType = group.GetAttributeValue("data-id", "softsub").ToUpperInvariant();
            var serverNodes = group.SelectNodes(".//span[contains(@class, 'server')]");
            if (serverNodes is null)
                continue;

            foreach (var serverNode in serverNodes)
            {
                var linkId = serverNode.GetAttributeValue("data-lid", "");
                var serverName = serverNode.InnerText.Trim();

                if (string.IsNullOrWhiteSpace(linkId))
                    continue;

                // Fetch the actual embed URL for this server
                var linkToken = await _megaUp.GenerateTokenAsync(linkId, cancellationToken);
                var linkUrl = $"{BaseUrl}/ajax/links/view?id={linkId}&_={linkToken}";
                var linkResponse = await _http.ExecuteAsync(
                    linkUrl,
                    GetAjaxHeaders(referer),
                    cancellationToken
                );

                var linkResult = ParseResultString(linkResponse);
                if (string.IsNullOrWhiteSpace(linkResult))
                    continue;

                // Decode the iframe data to get the video URL
                var (videoUrl, _, _) = await _megaUp.DecodeIframeDataAsync(
                    linkResult!,
                    cancellationToken
                );

                if (string.IsNullOrWhiteSpace(videoUrl))
                    continue;

                servers.Add(
                    new VideoServer
                    {
                        Name = $"{serverName} ({groupType})",
                        Embed = new FileUrl(videoUrl)
                        {
                            Headers = new Dictionary<string, string> { ["Referer"] = BaseUrl },
                        },
                    }
                );
            }
        }

        return servers;
    }

    /// <inheritdoc />
    public override IVideoExtractor? GetVideoExtractor(VideoServer server)
    {
        // All AniKai servers use MegaUp
        return new MegaUpExtractor(_httpClientFactory);
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Parses the "result" field from a JSON AJAX response as HTML content.
    /// Handles both raw JSON and HTML-wrapped JSON responses.
    /// </summary>
    private static string? ParseResultHtml(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        try
        {
            // Try parsing as JSON first
            var json = JsonNode.Parse(response);
            return json?["result"]?.ToString();
        }
        catch
        {
            // If it starts with HTML, try extracting JSON from <pre> tag
            if (response!.TrimStart().StartsWith("<"))
            {
                var doc = Html.Parse(response);
                var pre = doc.DocumentNode.SelectSingleNode("//pre");
                if (pre is not null)
                {
                    try
                    {
                        var json = JsonNode.Parse(pre.InnerText);
                        return json?["result"]?.ToString();
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Parses the "result" field from a JSON AJAX response as a string value.
    /// </summary>
    private static string? ParseResultString(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        try
        {
            var json = JsonNode.Parse(response);
            return json?["result"]?.ToString();
        }
        catch
        {
            if (response!.TrimStart().StartsWith("<"))
            {
                var doc = Html.Parse(response);
                var pre = doc.DocumentNode.SelectSingleNode("//pre");
                if (pre is not null)
                {
                    try
                    {
                        var json = JsonNode.Parse(pre.InnerText);
                        return json?["result"]?.ToString();
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            return null;
        }
    }

    #endregion
}
