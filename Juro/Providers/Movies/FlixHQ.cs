﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Juro.Extractors;
using Juro.Models.Movies;
using Juro.Models.Videos;
using Juro.Utils.Extensions;
using Newtonsoft.Json.Linq;

namespace Juro.Providers.Movies;

public class FlixHQ : MovieParser
{
    public override string Name { get; set; } = "FlixHQ";

    public override string BaseUrl => "https://flixhq.to";

    public override string Logo => "https://img.flixhq.to/xxrz/400x400/100/ab/5f/ab5f0e1996cc5b71919e10e910ad593e/ab5f0e1996cc5b71919e10e910ad593e.png";

    public FlixHQ(HttpClient httpClient) : base(httpClient)
    {
    }

    public override Task<List<MovieResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(query, 1, cancellationToken);
    }

    public async Task<List<MovieResult>> SearchAsync(
        string query,
        int page = 1,
        CancellationToken cancellationToken = default!)
    {
        //query = Regex.Replace(query, @"/[\W_]+/g", "-");
        query = query.Replace(" ", "-");

        var response = await _http.ExecuteAsync($"{BaseUrl}/search/{query}?page={page}", cancellationToken);

        var document = new HtmlDocument();
        document.LoadHtml(response);

        var nodes = document.DocumentNode.SelectNodes(".//div[@class='film_list-wrap']/div[@class='flw-item']").ToList();

        var movies = new List<MovieResult>();
        foreach (var node in nodes)
        {
            var releasedDate = document.DocumentNode.SelectSingleNode(".//div[@class='film-detail']/div[@class='fd-infor']/span[1]")?.InnerText;

            movies.Add(new()
            {
                Id = node.SelectSingleNode(".//div[@class='film-poster']/a")?.Attributes["href"]?.Value.Substring(1) ?? string.Empty,
                Title = node.SelectSingleNode(".//div[@class='film-detail']/h2/a")?.Attributes["title"]?.Value,
                Url = $"{BaseUrl}{node.SelectSingleNode(".//div[@class='film-poster']/a")?.Attributes["href"]?.Value}",
                Image = node.SelectSingleNode(".//div[@class='film-poster']/img")?.Attributes["data-src"]?.Value,
                ReleasedDate = releasedDate,
                Type = document.DocumentNode.SelectSingleNode(".//div[@class='film-detail']/div[@class='fd-infor']/span[contains(@class, 'float-right')]")?.InnerText?.ToLower() == "movie"
                    ? TvType.Movie : TvType.TvSeries
            });
        }

        return movies;
    }

    public override async Task<MovieInfo> GetMediaInfoAsync(
        string mediaId,
        CancellationToken cancellationToken = default!)
    {
        if (string.IsNullOrEmpty(mediaId))
            return new();

        if (!mediaId.StartsWith(BaseUrl))
            mediaId = $"{BaseUrl}/{mediaId}";

        var movieInfo = new MovieInfo()
        {
            Id = new Stack<string>(mediaId.Split(new[] { "to/" }, StringSplitOptions.None)).Pop(),
        };

        var response = await _http.ExecuteAsync(mediaId, cancellationToken);
        response = WebUtility.HtmlDecode(response);

        var document = new HtmlDocument();
        document.LoadHtml(response);

        var uid = document.DocumentNode.SelectSingleNode(".//div[contains(@class, 'watch_block')]")!.Attributes["data-id"]!.Value;
        movieInfo.Title = document.DocumentNode.Descendants().Where(x => x?.HasClass("heading-name") == true).FirstOrDefault()?.InnerText;
        movieInfo.Image = document.DocumentNode.SelectSingleNode(".//div[contains(@class, 'm_i-d-poster')]/div/img")?.Attributes["src"]?.Value;
        movieInfo.Description = document.DocumentNode.SelectSingleNode(".//div[@class='description']")?.InnerText?.Trim();
        movieInfo.Type = movieInfo.Id.ToLower().Split('/')[0] == "tv" ? TvType.TvSeries : TvType.Movie;
        movieInfo.ReleasedDate = document.DocumentNode.SelectNodes(".//div[@class='row-line']")?[2]?.InnerText?.Replace("Released: ", "")?.Trim();
        movieInfo.Genres = document.DocumentNode.SelectNodes(".//div[@class='row-line'][2]/a").SelectMany(x => x.InnerText.Split(new[] { "&" }, StringSplitOptions.None))
            .Select(x => x.Trim()).ToList();
        movieInfo.Casts = document.DocumentNode.SelectNodes(".//div[@class='row-line'][5]/a")
            .Select(x => x.InnerText.Trim()).ToList();
        movieInfo.Tags = document.DocumentNode.SelectNodes(".//div[@class='row-line'][6]/h2")
            .Select(x => x.InnerText.Trim()).ToList();
        movieInfo.Production = document.DocumentNode.SelectSingleNode(".//div[@class='row-line'][4]/a[2]")?.InnerText?.Trim();
        movieInfo.Country = document.DocumentNode.SelectSingleNode(".//div[@class='row-line'][1]/a[2]")?.InnerText?.Trim();
        movieInfo.Duration = document.DocumentNode.SelectSingleNode(".//span[contains(@class, 'item')][3]")?.InnerText?.Trim();
        movieInfo.Rating = document.DocumentNode.SelectSingleNode(".//span[contains(@class, 'item')][2]")?.InnerText?.Trim();

        if (movieInfo.Type == TvType.TvSeries)
        {
            var ajaxReqUrl = GetAjaxReqUrl(uid, "tv", true);
            response = await _http.ExecuteAsync(ajaxReqUrl, cancellationToken);

            document = new();
            document.LoadHtml(response);

            var seasonsIds = document.DocumentNode.SelectNodes(".//div[contains(@class, 'dropdown-menu')]/a")!.Select(x => x.Attributes["data-id"]!.Value).ToList();

            movieInfo.Episodes = new();

            var season = 1;

            foreach (var id in seasonsIds)
            {
                ajaxReqUrl = GetAjaxReqUrl(id, "season");
                response = await _http.ExecuteAsync(ajaxReqUrl, cancellationToken);
                response = WebUtility.HtmlDecode(response);

                document = new();
                document.LoadHtml(response);

                var nodes = document.DocumentNode.SelectNodes(".//ul[contains(@class, 'nav')]/li").ToList();

                for (int i = 0; i < nodes.Count; i++)
                {
                    movieInfo.Episodes.Add(new()
                    {
                        Id = nodes[i].SelectSingleNode(".//a").Attributes["id"].Value.Split('-')[1],
                        Title = nodes[i].SelectSingleNode(".//a").Attributes["id"].Value.Split('-')[1],
                        Number = Convert.ToInt32(nodes[i].SelectSingleNode(".//a").Attributes["title"].Value.Split(':')[0].Substring(3).Trim()),
                        Season = season,
                        Url = $"{BaseUrl}/ajax/v2/episode/servers/{nodes[i].SelectSingleNode(".//a").Attributes["id"].Value.Split('-')[1]}"
                    });
                }

                season++;
            }
        }
        else
        {
            movieInfo.Episodes = new()
            {
                new()
                {
                    Id = uid,
                    Title = movieInfo.Title + " Movie",
                    Url = $"{BaseUrl}/ajax/movie/episodes/{uid}"
                }
            };
        }

        return movieInfo;
    }

    private string GetAjaxReqUrl(string id, string type, bool isSeasons = false)
    {
        return $"{BaseUrl}/ajax/{(type == "movie" ? type : $"v2/{type}")}/{(isSeasons ? "seasons" : "episodes")}/{id}";
    }

    public override async Task<List<EpisodeServer>> GetEpisodeServersAsync(
        string episodeId,
        string mediaId,
        CancellationToken cancellationToken = default!)
    {
        if (!episodeId.StartsWith(BaseUrl + "/ajax") && !mediaId.Contains("movie"))
            episodeId = $"{BaseUrl}/ajax/v2/episode/servers/{episodeId}";
        else episodeId = $"{BaseUrl}/ajax/movie/episodes/{episodeId}";

        var servers = new List<EpisodeServer>();

        var response = await _http.ExecuteAsync(episodeId, cancellationToken);

        var document = new HtmlDocument();
        document.LoadHtml(response);

        var nodes = document.DocumentNode.SelectNodes(".//ul[contains(@class, 'nav')]/li").ToList();

        for (int i = 0; i < nodes.Count; i++)
        {
            servers.Add(new()
            {
                Name = mediaId.Contains("movie")
                    ? nodes[i].SelectSingleNode(".//a").Attributes["title"].Value.ToLower()
                    : nodes[i].SelectSingleNode(".//a").Attributes["title"].Value.Substring(6).Trim().ToLower(),
                Url = $"{BaseUrl}/{mediaId}.{(!mediaId.Contains("movie") ? nodes[i].SelectSingleNode(".//a").Attributes["data-id"].Value : nodes[i].SelectSingleNode(".//a").Attributes["data-linkid"].Value)}"
                    .Replace(!mediaId.Contains("movie") ? "/tv/" : "/movie/", !mediaId.Contains("movie") ? "/watch-tv/" : "/watch-movie/")
            });
        }

        return servers;
    }

    public override async Task<List<VideoSource>> GetEpisodeSourcesAsync(
        string episodeId,
        string mediaId,
        StreamingServers server = StreamingServers.UpCloud,
        CancellationToken cancellationToken = default!)
    {
        var serverUrl = episodeId;

        if (episodeId.StartsWith("http"))
        {
            return server switch
            {
                StreamingServers.MixDrop => new(),
                StreamingServers.UpCloud => await new VidCloud(_http).ExtractAsync(serverUrl, cancellationToken),
                StreamingServers.VidCloud => new(),
                _ => await new VidCloud(_http).ExtractAsync(serverUrl, cancellationToken),
            };
        }

        var servers = await GetEpisodeServersAsync(episodeId, mediaId, cancellationToken);

        var serverIndex = servers.FindIndex(x => x.Name.ToLower() == Enum.GetName(typeof(StreamingServers), server)!.ToLower());

        if (serverIndex == -1)
            throw new Exception($"Server {server} not found");

        //server = (StreamingServers)Enum.Parse(typeof(StreamingServers), episodeServer.Name);

        var url = $"{BaseUrl}/ajax/get_link/{servers[serverIndex].Url.Split('.').LastOrDefault()}";
        var response = await _http.ExecuteAsync(url, cancellationToken);

        serverUrl = JObject.Parse(response)["link"]!.ToString();

        return await GetEpisodeSourcesAsync(serverUrl, mediaId, server, cancellationToken);
    }
}