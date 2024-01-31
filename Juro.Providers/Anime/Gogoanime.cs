﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
/// Client for interacting with gogoanime.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="Gogoanime"/>.
/// </remarks>
public class Gogoanime(IHttpClientFactory httpClientFactory)
    : AnimeBaseProvider(httpClientFactory),
        IAnimeProvider,
        IPopularProvider,
        ILastUpdatedProvider,
        INewSeasonProvider
{
    private readonly HttpClient _http = httpClientFactory.CreateClient();

    public string Key => Name;

    public string Name => "Gogo";

    public string Language => "en";

    public bool IsDubAvailableSeparately => true;

    public string BaseUrl { get; private set; } = default!;

    public string CdnUrl { get; private set; } = default!;

    /// <summary>
    /// Initializes an instance of <see cref="Gogoanime"/>.
    /// </summary>
    public Gogoanime(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="Gogoanime"/>.
    /// </summary>
    public Gogoanime()
        : this(Http.ClientProvider) { }

    private async ValueTask LoadUrlsAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(BaseUrl))
            return;

        var response = await _http.ExecuteAsync(
            "https://raw.githubusercontent.com/jerry08/anistream-extras/main/gogoanime.json",
            cancellationToken
        );

        if (!string.IsNullOrWhiteSpace(response))
        {
            var data = JsonNode.Parse(response)!;

            BaseUrl = data["base_url"]!.ToString();
            CdnUrl = data["cdn_url"]!.ToString();
        }
    }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        return await SearchAsync(query, false, cancellationToken);
    }

    /// <inheritdoc cref="IAnimeProvider.SearchAsync"/>
    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        bool selectDub,
        CancellationToken cancellationToken = default
    )
    {
        await LoadUrlsAsync(cancellationToken);

        //query = selectDub ? query + "%(Dub)" : query;
        //query = query.Replace(" ", "+");

        query = selectDub ? query + " (Dub)" : query;
        query = Uri.EscapeDataString(query);

        var response = await _http.ExecuteAsync(
            $"{BaseUrl}search.html?keyword={query}",
            cancellationToken
        );

        return ParseAnimeResponse(response);
    }

    /// <inheritdoc cref="IAnimeProvider.SearchAsync"/>
    public async ValueTask<List<IAnimeInfo>> GetPopularAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        await LoadUrlsAsync(cancellationToken);

        var response = await _http.ExecuteAsync(
            $"{BaseUrl}popular.html?page={page}",
            cancellationToken
        );

        return ParseAnimeResponse(response);
    }

    /// <inheritdoc cref="IAnimeProvider.SearchAsync"/>
    public async ValueTask<List<IAnimeInfo>> GetNewSeasonAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        await LoadUrlsAsync(cancellationToken);

        var response = await _http.ExecuteAsync(
            $"{BaseUrl}new-season.html?page={page}",
            cancellationToken
        );

        return ParseAnimeResponse(response);
    }

    /// <inheritdoc cref="IAnimeProvider.SearchAsync"/>
    public async ValueTask<List<IAnimeInfo>> GetLastUpdatedAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        await LoadUrlsAsync(cancellationToken);

        var response = await _http.ExecuteAsync($"{BaseUrl}?page={page}", cancellationToken);

        return ParseAnimeResponse(response);
    }

    private List<IAnimeInfo> ParseAnimeResponse(string response)
    {
        var animes = new List<IAnimeInfo>();

        if (string.IsNullOrWhiteSpace(response))
            return animes;

        var document = Html.Parse(response);

        var itemsNode = document
            .DocumentNode.Descendants()
            .FirstOrDefault(node => node.HasClass("items"));

        if (itemsNode is not null)
        {
            var nodes = itemsNode.Descendants().Where(node => node.Name == "li").ToList();
            for (var i = 0; i < nodes.Count; i++)
            {
                var img = "";
                var title = "";
                var category = "";
                var released = "";
                var link = "";

                var imgNode = nodes[i].SelectSingleNode(".//div[@class='img']/a/img");
                if (imgNode is not null)
                    img = imgNode.Attributes["src"].Value;

                var nameNode = nodes[i].SelectSingleNode(".//p[@class='name']/a");
                if (nameNode is not null)
                {
                    category = nameNode.Attributes["href"].Value;
                    title = nameNode.Attributes["title"].Value;

                    if (string.IsNullOrWhiteSpace(title))
                    {
                        title = nameNode.InnerText;

                        if (title.StartsWith(@""""))
                            title = title.Substring(1);

                        if (title.EndsWith(@""""))
                            title = title.Substring(0, title.Length - 1);
                    }
                }

                var releasedNode = nodes[i].SelectSingleNode(".//p[@class='released']");
                if (releasedNode is not null)
                    released = new string(releasedNode.InnerText.Where(char.IsDigit).ToArray());

                var id = category.Contains("-episode")
                    ? "/category" + category.Remove(category.LastIndexOf("-episode"))
                    : category;

                link = BaseUrl + category;

                animes.Add(
                    new AnimeInfo()
                    {
                        Id = id,
                        Site = AnimeSites.GogoAnime,
                        Image = img,
                        Title = title,
                        Category = category,
                        Released = released,
                        Link = link,
                    }
                );
            }
        }

        return animes;
    }

    /// <inheritdoc />
    public async ValueTask<IAnimeInfo> GetAnimeInfoAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        await LoadUrlsAsync(cancellationToken);

        // Exceptions
        if (id.Contains("/jujutsu-kaisen-2nd-season"))
        {
            id = id.Replace("jujutsu-kaisen-2nd-season", "jujutsu-kaisen-tv-2nd-season");
        }

        var url = BaseUrl + id;

        var anime = new AnimeInfo() { Id = id };

        if (id.Contains("-episode"))
        {
            var epsResponse = await _http.ExecuteAsync(url, cancellationToken);

            var epsDocument = Html.Parse(epsResponse);

            url = epsDocument
                .DocumentNode.SelectSingleNode(".//div[@class='anime-info']/a")
                ?.Attributes["href"]
                ?.Value;

            if (url is null)
                return anime;

            url = BaseUrl + url;
        }

        if (!id.StartsWith("/category"))
        {
            url = $"{BaseUrl}/category{id}";
        }

        var response = await _http.ExecuteAsync(url, cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
            return anime;

        anime.Category = url;

        var document = Html.Parse(response);

        var animeInfoNodes = document
            .DocumentNode.SelectNodes(".//div[@class='anime_info_body_bg']/p")
            .ToList();

        var imgNode = document.DocumentNode.SelectSingleNode(
            ".//div[@class='anime_info_body_bg']/img"
        );
        if (imgNode is not null)
            anime.Image = imgNode.Attributes["src"].Value;

        var titleNode = document.DocumentNode.SelectSingleNode(
            ".//div[@class='anime_info_body_bg']/h1"
        );
        if (titleNode is not null)
            anime.Title = titleNode.InnerText;

        if (anime.Title.StartsWith(@""""))
            anime.Title = anime.Title.Substring(1);

        if (anime.Title.EndsWith(@""""))
            anime.Title = anime.Title.Substring(0, anime.Title.Length - 1);

        for (var i = 0; i < animeInfoNodes.Count; i++)
        {
            switch (i)
            {
                case 0: //Bookmarks
                    break;
                case 1: //Type (e.g TV Series)
                    anime.Type = Regex.Replace(animeInfoNodes[i].InnerText, @"\t|\n|\r", "");
                    anime.Type = new Regex("[ ]{2,}", RegexOptions.None)
                        .Replace(anime.Type, " ")
                        .Trim();
                    anime.Type = anime.Type.Trim();
                    break;
                case 2: //Plot SUmmary
                    anime.Summary = animeInfoNodes[i].InnerText.Trim();
                    break;
                case 3: //Genre
                    var genres = animeInfoNodes[i].InnerText.Replace("Genre:", "").Trim().Split(',');
                    foreach (var genre in genres)
                        anime.Genres.Add(new(genre));
                    break;
                case 4: //Released Year
                    anime.Released = Regex.Replace(animeInfoNodes[i].InnerText, @"\t|\n|\r", "");
                    anime.Released = new Regex("[ ]{2,}", RegexOptions.None)
                        .Replace(anime.Released, " ")
                        .Trim();
                    break;
                case 5: //Status
                    anime.Status = Regex.Replace(animeInfoNodes[i].InnerText, @"\t|\n|\r", "");
                    anime.Status = new Regex("[ ]{2,}", RegexOptions.None)
                        .Replace(anime.Status, " ")
                        .Trim();
                    anime.Status = anime.Status.Trim();
                    break;
                case 6: //Other Name
                    anime.OtherNames = Regex.Replace(animeInfoNodes[i].InnerText, @"\t|\n|\r", "");
                    anime.OtherNames = new Regex("[ ]{2,}", RegexOptions.None)
                        .Replace(anime.OtherNames, " ")
                        .Trim();
                    break;
                default:
                    break;
            }
        }

        return anime;
    }

    /// <inheritdoc />
    public async ValueTask<List<Episode>> GetEpisodesAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        // Exceptions
        if (id.Contains("/jujutsu-kaisen-2nd-season"))
        {
            id = id.Replace("jujutsu-kaisen-2nd-season", "jujutsu-kaisen-tv-2nd-season");
        }

        await LoadUrlsAsync(cancellationToken);

        var episodes = new List<Episode>();

        var response = await _http.ExecuteAsync(BaseUrl + id, cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
            return episodes;

        var document = Html.Parse(response);

        var lastEpisode = document
            .DocumentNode.Descendants()
            .LastOrDefault(x => x.Attributes["ep_end"] is not null)
            ?.Attributes["ep_end"]
            .Value;

        var animeId = document
            .DocumentNode.Descendants()
            .FirstOrDefault(x => x.Id == "movie_id")
            ?.Attributes["value"]
            .Value;

        var url =
            $"https://ajax.gogo-load.com/ajax/load-list-episode?ep_start=0&ep_end={lastEpisode}&id={animeId}";
        //response = await _http.ExecuteAsync(CdnUrl + animeId, cancellationToken);
        response = await _http.ExecuteAsync(url, cancellationToken);

        document = Html.Parse(response);

        var liNodes = document.DocumentNode.Descendants().Where(node => node.Name == "li").ToList();

        for (var i = 0; i < liNodes.Count; i++)
        {
            var epName = "";
            var href = "";
            var subOrDub = "";

            var hrefNode = liNodes[i].SelectSingleNode(".//a");
            if (hrefNode is not null)
                href = hrefNode.Attributes["href"].Value.Trim();

            var nameNode = liNodes[i].SelectSingleNode(".//div[@class='name']");
            if (nameNode is not null)
                epName = nameNode.InnerText;

            var subDubNode = liNodes[i].SelectSingleNode(".//div[@class='cate']");
            if (subDubNode is not null)
            {
                //subOrDub = subDubNode.Attributes["title"].Value; //OR name = nameNode.InnerText;
                subOrDub = subDubNode.InnerText;
            }

            //var epNumber = Convert.ToSingle(link.Split(new char[] { '-' }).LastOrDefault());
            var epNumber = float.Parse(epName.ToLower().Replace("ep", "").Trim());

            episodes.Add(
                new Episode()
                {
                    Id = href,
                    Link = BaseUrl + href,
                    Number = epNumber,
                    Name = epName,
                }
            );
        }

        return episodes;
    }

    private static string HttpsIfy(string text) =>
        string.Join("", text.Take(2)) == "//" ? $"https:{text}" : text;

    /// <inheritdoc />
    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    )
    {
        await LoadUrlsAsync(cancellationToken);

        var episodeUrl = BaseUrl + episodeId;

        var response = await _http.ExecuteAsync(episodeUrl, cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
            return [];

        var document = Html.Parse(response);

        //Exception for fire force season 2 episode 1
        if (response.Contains(">404</h1>"))
            response = await _http.ExecuteAsync(episodeUrl + "-1", cancellationToken);

        var list = new List<VideoServer>();

        var servers = document.DocumentNode.SelectNodes(".//div[@class='anime_muti_link']/ul/li");
        for (var i = 0; i < servers.Count; i++)
        {
            var name = servers[i]
                .SelectSingleNode("a")
                .InnerText.Replace("Choose this server", "")
                .Trim();
            var url = HttpsIfy(servers[i].SelectSingleNode("a").Attributes["data-video"].Value);

            list.Add(new VideoServer(name, new FileUrl(url)));
        }

        return list;
    }

    public async ValueTask<List<Genre>> GetGenresAsync(CancellationToken cancellationToken)
    {
        await LoadUrlsAsync(cancellationToken);

        var genres = new List<Genre>();

        var response = await _http.ExecuteAsync(BaseUrl, cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
            return genres;

        var document = Html.Parse(response);

        var genresNode = document
            .DocumentNode.Descendants()
            .FirstOrDefault(node => node.GetClasses().Contains("genre"));

        if (genresNode is not null)
        {
            var nodes = genresNode.Descendants().Where(node => node.Name == "li").ToList();
            for (var i = 0; i < nodes.Count; i++)
            {
                var name = "";
                var link = "";

                var nameNode = nodes[i].SelectSingleNode(".//a");
                if (nameNode is not null)
                {
                    link = nameNode.Attributes["href"].Value;
                    name = nameNode.Attributes["title"].Value; //OR name = nameNode.InnerText;
                }

                genres.Add(new Genre(name, link));
            }
        }

        return genres;
    }
}
