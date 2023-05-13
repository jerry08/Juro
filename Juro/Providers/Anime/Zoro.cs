using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Extractors;
using Juro.Models;
using Juro.Models.Anime;
using Juro.Models.Videos;
using Juro.Utils;
using Juro.Utils.Extensions;
using Nager.PublicSuffix;
using Newtonsoft.Json.Linq;

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with Zoro.
/// </summary>
public class Zoro : IAnimeProvider
{
    private readonly HttpClient _http;
    private readonly Func<HttpClient> _httpClientProvider;

    public string Name => "Zoro";

    public bool IsDubAvailableSeparately => true;

    public string BaseUrl => "https://zoro.to";

    /// <summary>
    /// Initializes an instance of <see cref="Zoro"/>.
    /// </summary>
    public Zoro(Func<HttpClient> httpClientProvider)
    {
        _http = httpClientProvider();
        _httpClientProvider = httpClientProvider;
    }

    /// <summary>
    /// Initializes an instance of <see cref="Zoro"/>.
    /// </summary>
    public Zoro() : this(Http.ClientProvider)
    {
    }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        query = query.Replace(" ", "+");

        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/search?keyword={query}",
            cancellationToken
        );

        return ParseAnimeResponse(response);
    }

    /// <inheritdoc cref="SearchAsync"/>
    public async ValueTask<List<IAnimeInfo>> GetPopularAsync(
        int page = 1,
        CancellationToken cancellationToken = default)
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
        CancellationToken cancellationToken = default)
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
        CancellationToken cancellationToken = default)
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

        var nodes = document.DocumentNode.Descendants()
            .Where(node => node.HasClass("flw-item")).ToList();

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

            var nameNode = nodes[i].SelectSingleNode(".//div[@class='film-detail']")
                .SelectSingleNode(".//a");
            if (nameNode is not null)
            {
                category = nameNode.Attributes["href"].Value;
                title = nameNode.Attributes["title"].Value; //OR name = nameNode.InnerText;
            }

            animes.Add(new AnimeInfo()
            {
                Id = category,
                Site = AnimeSites.Zoro,
                Image = img,
                Title = title,
                Category = category,
                Link = BaseUrl + category
            });
        }

        return animes;
    }

    /// <inheritdoc />
    public async ValueTask<IAnimeInfo> GetAnimeInfoAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var dataId = id.Split('-').Last().Split('?')[0];
        var url = $"{BaseUrl}/ajax/v2/episode/list/{dataId}";

        //Get anime details
        var response = await _http.ExecuteAsync(BaseUrl + id, cancellationToken);
        //https://stackoverflow.com/questions/122641/how-can-i-decode-html-characters-in-c
        //HttpUtility.HtmlDecode();

        var anime = new AnimeInfo()
        {
            Id = id,
            Site = AnimeSites.Zoro
        };

        if (string.IsNullOrWhiteSpace(response))
            return anime;

        var document = Html.Parse(response);

        anime.Title = document.DocumentNode
            .SelectSingleNode(".//h2[contains(@class, 'film-name')]")?
            .InnerText ?? "";

        anime.Summary = document.DocumentNode.SelectSingleNode(".//div[contains(@class, 'film-description')]")?
            .InnerText?.Trim() ?? "";

        anime.Image = document.DocumentNode.SelectSingleNode(".//img[contains(@class, 'film-poster-img')]")?
            .Attributes["src"]?.Value?.ToString() ?? "";

        var itemHeadNodes = document.DocumentNode.SelectNodes(".//div[@class='anisc-info-wrap']/div[@class='anisc-info']//span[@class='item-head']");
        //var overviewNode = document.DocumentNode.SelectNodes(".//div[@class='anisc-info-wrap']/div[@class='anisc-info']")[0];
        //anime.Summary = overviewNode.InnerText;

        var overviewNode = itemHeadNodes.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.InnerHtml)
            && x.InnerHtml.ToLower().Contains("overview"))?
            .ParentNode.SelectSingleNode(".//span[@class='name']")
            ?? itemHeadNodes.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.InnerHtml)
            && x.InnerHtml.ToLower().Contains("overview"))?
            .ParentNode.SelectSingleNode(".//div[@class='text']");
        if (overviewNode is not null)
            anime.Summary = overviewNode.InnerText.Trim();

        var typeNode = document.DocumentNode.SelectNodes(".//div[@class='film-stats']//span[@class='dot']")?
            .FirstOrDefault()?.NextSibling.NextSibling;
        if (typeNode is not null)
            anime.Type = typeNode.InnerText;

        var statusNode = itemHeadNodes.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.InnerHtml)
            && x.InnerHtml.ToLower().Contains("status"))?
            .ParentNode.SelectSingleNode(".//span[@class='name']");
        if (statusNode is not null)
            anime.Status = statusNode.InnerText;

        var genresNode = itemHeadNodes.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.InnerHtml)
            && x.InnerHtml.ToLower().Contains("genres"))?
            .ParentNode.SelectNodes(".//a").ToList();
        if (genresNode is not null)
            anime.Genres.AddRange(genresNode.Select(x => new Genre(x.Attributes["title"].Value)));

        var airedNode = itemHeadNodes.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.InnerHtml)
            && x.InnerHtml.ToLower().Contains("aired"))?
            .ParentNode.SelectSingleNode(".//span[@class='name']");
        if (airedNode is not null)
            anime.Released = airedNode.InnerText;

        var synonymsNode = itemHeadNodes.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.InnerHtml)
            && x.InnerHtml.ToLower().Contains("synonyms"))?
            .ParentNode.SelectSingleNode(".//span[@class='name']");
        if (synonymsNode is not null)
            anime.OtherNames = synonymsNode.InnerText;

        return anime;
    }

    /// <inheritdoc />
    public async ValueTask<List<Episode>> GetEpisodesAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var dataId = id.Split('-').Last().Split('?')[0];
        var url = $"{BaseUrl}/ajax/v2/episode/list/{dataId}";

        //Get anime episodes
        var json = await _http.ExecuteAsync(url, cancellationToken);
        var jObj = JObject.Parse(json);
        var response = jObj["html"]!.ToString();

        var document = Html.Parse(response);

        var nodes = document.DocumentNode.SelectNodes(".//a")
            .Where(x => x.Attributes["data-page"] == null).ToList();

        var episodes = new List<Episode>();
        for (var i = 0; i < nodes.Count; i++)
        {
            var title = nodes[i].Attributes["title"].Value;
            var dataNumber = Convert.ToInt32(nodes[i].Attributes["data-number"].Value);
            var dataId2 = nodes[i].Attributes["data-id"].Value;
            var link = nodes[i].Attributes["href"].Value;

            episodes.Add(new Episode()
            {
                Id = link,
                Name = $"{i + 1} - {title}",
                Link = link,
                Number = dataNumber
            });
        }

        return episodes;
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default)
        => await GetVideoServersAsync(episodeId, SubDub.All, cancellationToken);

    /// <inheritdoc cref="GetVideoServersAsync"/>
    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        SubDub subDub,
        CancellationToken cancellationToken = default)
    {
        var dataId = episodeId.Split(new[] { "ep=" }, StringSplitOptions.None).Last();

        var url = $"{BaseUrl}/ajax/v2/episode/servers?episodeId={dataId}";
        var response = await _http.ExecuteAsync(url, cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
            return new();

        var data = JObject.Parse(response);

        var doc = Html.Parse(data["html"]!.ToString());

        var nodes = doc.DocumentNode.SelectNodes(".//div[contains(@class, 'server-item')]")
            .ToList();

        var videoServers = new List<VideoServer>();

        for (var i = 0; i < nodes.Count; i++)
        {
            dataId = nodes[i].Attributes["data-id"].Value;
            var dataType = nodes[i].Attributes["data-type"].Value.ToLower().Trim();
            var serverName = $"({dataType.ToUpper()}) {nodes[i].InnerText.Trim()}";

            if (subDub != SubDub.All)
            {
                if (dataType == "dub" && subDub != SubDub.Dub)
                    continue;

                if (dataType == "sub" && subDub != SubDub.Sub)
                    continue;
            }

            url = $"https://zoro.to/ajax/v2/episode/sources?id={dataId}";
            response = await _http.ExecuteAsync(url, cancellationToken);

            data = JObject.Parse(response);
            //var type = data["type"]!.ToString();
            //var server = data["server"]!.ToString();

            var link = data["link"]!.ToString();
            var embedHeaders = new Dictionary<string, string>()
            {
                { "Referer", BaseUrl + "/" }
            };

            videoServers.Add(new VideoServer(serverName, new FileUrl(link, embedHeaders)));
        }

        return videoServers;
    }

    public IVideoExtractor? GetVideoExtractor(VideoServer server)
    {
        var domainParser = new DomainParser(new WebTldRuleProvider());
        var domainInfo = domainParser.Parse(server.Embed.Url);

        if (domainInfo.Domain.Contains("rapid"))
        {
            return new RapidCloud(_httpClientProvider);
        }
        else if (domainInfo.Domain.Contains("sb"))
        {
            return new StreamSB(_httpClientProvider);
        }
        else if (domainInfo.Domain.Contains("streamta"))
        {
            return new StreamTape(_httpClientProvider);
        }

        return null;
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoSource>> GetVideosAsync(
        VideoServer server,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.IsWellFormedUriString(server.Embed.Url, UriKind.Absolute))
            return new();

        var extractor = GetVideoExtractor(server);
        if (extractor is null)
            return new();

        return await extractor.ExtractAsync(server.Embed.Url, cancellationToken);
    }
}