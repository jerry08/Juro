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
using Juro.Core.Utils.Tasks;

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with Aniwatch.
/// </summary>
public class Aniwatch
    : AnimeBaseProvider,
        IAnimeProvider,
        IPopularProvider,
        IAiringProvider,
        IRecentlyAddedProvider
{
    private readonly HttpClient _http;

    public virtual string Key => Name;

    public virtual string Name => "Aniwatch";

    public string Language => "en";

    public bool IsDubAvailableSeparately => true;

    public virtual string BaseUrl => "https://aniwatch.to";

    public virtual string AjaxUrl => $"{BaseUrl}/ajax/v2";

    protected virtual AnimeSites AnimeSite => AnimeSites.Aniwatch;

    /// <summary>
    /// Initializes an instance of <see cref="Aniwatch"/>.
    /// </summary>
    public Aniwatch(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory)
    {
        _http = httpClientFactory.CreateClient();
    }

    /// <summary>
    /// Initializes an instance of <see cref="Aniwatch"/>.
    /// </summary>
    public Aniwatch(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="Aniwatch"/>.
    /// </summary>
    public Aniwatch()
        : this(Http.ClientProvider) { }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/search?keyword={Uri.EscapeDataString(query)}",
            cancellationToken
        );

        return ParseAnimeResponse(response);
    }

    /// <inheritdoc cref="SearchAsync"/>
    public async ValueTask<List<IAnimeInfo>> GetPopularAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/most-popular?page={page}",
            cancellationToken
        );

        return ParseAnimeResponse(response);
    }

    /// <summary>
    /// Gets anime in new season.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="cancellationToken"></param>
    public async ValueTask<List<IAnimeInfo>> GetRecentlyAddedAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/recently-added?page={page}",
            cancellationToken
        );

        return ParseAnimeResponse(response);
    }

    /// <inheritdoc cref="SearchAsync"/>
    public async ValueTask<List<IAnimeInfo>> GetAiringAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/top-airing?page={page}",
            cancellationToken
        );

        return ParseAnimeResponse(response);
    }

    private List<IAnimeInfo> ParseAnimeResponse(string? response)
    {
        var animes = new List<IAnimeInfo>();

        if (string.IsNullOrWhiteSpace(response))
            return animes;

        var document = Html.Parse(response!);

        var nodes = document.DocumentNode
            .Descendants()
            .Where(node => node.HasClass("flw-item"))
            .ToList();

        for (var i = 0; i < nodes.Count; i++)
        {
            var img = "";
            var title = "";
            var category = "";
            var dataId = "";

            var imgNode = nodes[i].SelectSingleNode(".//img");
            if (imgNode is not null)
                img = imgNode.Attributes["data-src"].Value;

            var dataIdNode = nodes[i].SelectSingleNode(".//a[@data-id]");
            if (dataIdNode is not null)
                dataId = dataIdNode.Attributes["data-id"].Value;

            var nameNode = nodes[i]
                .SelectSingleNode(".//div[@class='film-detail']")
                .SelectSingleNode(".//a");
            if (nameNode is not null)
            {
                category = nameNode.Attributes["href"].Value;
                title = nameNode.Attributes["title"].Value; //OR name = nameNode.InnerText;
            }

            animes.Add(
                new AnimeInfo()
                {
                    Id = category,
                    Site = AnimeSite,
                    Image = img,
                    Title = title,
                    Category = category,
                    Link = BaseUrl + category
                }
            );
        }

        return animes;
    }

    /// <inheritdoc />
    public async ValueTask<IAnimeInfo> GetAnimeInfoAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var dataId = id.Split('-').Last().Split('?')[0];
        var url = $"{AjaxUrl}/episode/list/{dataId}";

        //Get anime details
        var response = await _http.ExecuteAsync(BaseUrl + id, cancellationToken);
        //https://stackoverflow.com/questions/122641/how-can-i-decode-html-characters-in-c
        //HttpUtility.HtmlDecode();

        var anime = new AnimeInfo() { Id = id, Site = AnimeSite };

        if (string.IsNullOrWhiteSpace(response))
            return anime;

        var document = Html.Parse(response);

        anime.Title =
            document.DocumentNode
                .SelectSingleNode(".//h2[contains(@class, 'film-name')]")
                ?.InnerText ?? "";

        anime.Summary =
            document.DocumentNode
                .SelectSingleNode(".//div[contains(@class, 'film-description')]")
                ?.InnerText?.Trim() ?? "";

        anime.Image =
            document.DocumentNode
                .SelectSingleNode(".//img[contains(@class, 'film-poster-img')]")
                ?.Attributes["src"]?.Value?.ToString() ?? "";

        var itemHeadNodes = document.DocumentNode.SelectNodes(
            ".//div[@class='anisc-info-wrap']/div[@class='anisc-info']//span[@class='item-head']"
        );
        //var overviewNode = document.DocumentNode.SelectNodes(".//div[@class='anisc-info-wrap']/div[@class='anisc-info']")[0];
        //anime.Summary = overviewNode.InnerText;

        var overviewNode =
            itemHeadNodes
                .FirstOrDefault(
                    x =>
                        !string.IsNullOrWhiteSpace(x.InnerHtml)
                        && x.InnerHtml.ToLower().Contains("overview")
                )
                ?.ParentNode.SelectSingleNode(".//span[@class='name']")
            ?? itemHeadNodes
                .FirstOrDefault(
                    x =>
                        !string.IsNullOrWhiteSpace(x.InnerHtml)
                        && x.InnerHtml.ToLower().Contains("overview")
                )
                ?.ParentNode.SelectSingleNode(".//div[@class='text']");
        if (overviewNode is not null)
            anime.Summary = overviewNode.InnerText.Trim();

        var typeNode = document.DocumentNode
            .SelectNodes(".//div[@class='film-stats']//span[@class='dot']")
            ?.FirstOrDefault()
            ?.NextSibling.NextSibling;
        if (typeNode is not null)
            anime.Type = typeNode.InnerText;

        var statusNode = itemHeadNodes
            .FirstOrDefault(
                x =>
                    !string.IsNullOrWhiteSpace(x.InnerHtml)
                    && x.InnerHtml.ToLower().Contains("status")
            )
            ?.ParentNode.SelectSingleNode(".//span[@class='name']");
        if (statusNode is not null)
            anime.Status = statusNode.InnerText;

        var genresNode = itemHeadNodes
            .FirstOrDefault(
                x =>
                    !string.IsNullOrWhiteSpace(x.InnerHtml)
                    && x.InnerHtml.ToLower().Contains("genres")
            )
            ?.ParentNode.SelectNodes(".//a")
            .ToList();
        if (genresNode is not null)
            anime.Genres.AddRange(genresNode.Select(x => new Genre(x.Attributes["title"].Value)));

        var airedNode = itemHeadNodes
            .FirstOrDefault(
                x =>
                    !string.IsNullOrWhiteSpace(x.InnerHtml)
                    && x.InnerHtml.ToLower().Contains("aired")
            )
            ?.ParentNode.SelectSingleNode(".//span[@class='name']");
        if (airedNode is not null)
            anime.Released = airedNode.InnerText;

        var synonymsNode = itemHeadNodes
            .FirstOrDefault(
                x =>
                    !string.IsNullOrWhiteSpace(x.InnerHtml)
                    && x.InnerHtml.ToLower().Contains("synonyms")
            )
            ?.ParentNode.SelectSingleNode(".//span[@class='name']");
        if (synonymsNode is not null)
            anime.OtherNames = synonymsNode.InnerText;

        return anime;
    }

    /// <inheritdoc />
    public async ValueTask<List<Episode>> GetEpisodesAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var dataId = id.Split('-').Last().Split('?')[0];
        var url = $"{AjaxUrl}/episode/list/{dataId}";

        // Get anime episodes
        var json = await _http.ExecuteAsync(url, cancellationToken);
        var jObj = JsonNode.Parse(json)!;
        var response = jObj["html"]!.ToString();

        var document = Html.Parse(response);

        var nodes = document.DocumentNode
            .SelectNodes(".//a")
            .Where(x => x.Attributes["data-page"] == null)
            .ToList();

        var episodes = new List<Episode>();
        for (var i = 0; i < nodes.Count; i++)
        {
            var title = nodes[i].Attributes["title"].Value;
            var dataNumber = Convert.ToInt32(nodes[i].Attributes["data-number"].Value);
            var dataId2 = nodes[i].Attributes["data-id"].Value;
            var link = nodes[i].Attributes["href"].Value;

            episodes.Add(
                new Episode()
                {
                    Id = link,
                    Name = $"{i + 1} - {title}",
                    Link = link,
                    Number = dataNumber
                }
            );
        }

        return episodes;
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    ) => await GetVideoServersAsync(episodeId, SubDub.All, cancellationToken);

    /// <inheritdoc cref="IAnimeProvider.GetVideoServersAsync"/>
    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        SubDub subDub,
        CancellationToken cancellationToken = default
    )
    {
        var dataId = episodeId.Split(new[] { "ep=" }, StringSplitOptions.None).Last();

        var url = $"{AjaxUrl}/episode/servers?episodeId={dataId}";
        var response = await _http.ExecuteAsync(url, cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
            return new();

        var data = JsonNode.Parse(response)!;

        var doc = Html.Parse(data["html"]!.ToString());

        var nodes = doc.DocumentNode
            .SelectNodes(".//div[contains(@class, 'server-item')]")
            .ToList();

        var list = new List<VideoServer>();

        var functions = Enumerable
            .Range(0, nodes.Count)
            .Select(
                i =>
                    (Func<Task<VideoServer?>>)(
                        async () => await GetVideoServerAsync(nodes[i], subDub, cancellationToken)
                    )
            );

        var results = (await TaskEx.Run(functions, 10)).Where(x => x is not null).Select(x => x!);

        list.AddRange(results);

        return list;
    }

    private async ValueTask<VideoServer?> GetVideoServerAsync(
        HtmlNode node,
        SubDub subDub,
        CancellationToken cancellationToken = default
    )
    {
        var dataId = node.Attributes["data-id"].Value;
        var dataType = node.Attributes["data-type"].Value.ToLower().Trim();
        var serverName = $"({dataType.ToUpper()}) {node.InnerText.Trim()}";

        if (subDub != SubDub.All)
        {
            if (dataType == "dub" && subDub != SubDub.Dub)
                return null;

            if (dataType == "sub" && subDub != SubDub.Sub)
                return null;
        }

        var url = $"{AjaxUrl}/episode/sources?id={dataId}";
        var response = await _http.ExecuteAsync(url, cancellationToken);

        var data = JsonNode.Parse(response)!;
        //var type = data["type"]!.ToString();
        //var server = data["server"]!.ToString();

        var link = data["link"]!.ToString();
        var embedHeaders = new Dictionary<string, string>() { { "Referer", BaseUrl + "/" } };

        return new VideoServer(serverName, new FileUrl(link, embedHeaders));
    }
}
