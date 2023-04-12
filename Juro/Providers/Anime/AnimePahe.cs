using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Juro.Extractors;
using Juro.Models;
using Juro.Models.Anime;
using Juro.Models.Videos;
using Juro.Utils;
using Juro.Utils.Extensions;
using Juro.Utils.Tasks;
using Newtonsoft.Json.Linq;

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with AnimePahe.
/// </summary>
public class AnimePahe : IAnimeProvider
{
    private readonly HttpClient _http;
    private readonly Func<HttpClient> _httpClientProvider;

    public string Name => "AnimePahe";

    public bool IsDubAvailableSeparately => false;

    public string BaseUrl => "https://animepahe.com";

    private static readonly Regex _videoServerRegex = new("(.+) · (.+)p \\((.+)MB\\) ?(.*)");

    public AnimePahe(Func<HttpClient> httpClientProvider)
    {
        _http = httpClientProvider();
        _httpClientProvider = httpClientProvider;
    }

    public async Task<List<AnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var animes = new List<AnimeInfo>();

        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/api?m=search&q={Uri.EscapeUriString(query)}",
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(response))
            return animes;

        var data = JObject.Parse(response)["data"];
        if (data is null)
            return animes;

        return data.Select(x => new AnimeInfo()
        {
            Id = x["session"]!.ToString(),
            Title = x["title"]!.ToString(),
            Type = x["type"]!.ToString(),
            Episodes = int.TryParse(x["episodes"]?.ToString(), out var episodes) ? episodes : 0,
            Status = x["status"]!.ToString(),
            Season = x["season"]!.ToString(),
            Year = int.TryParse(x["year"]?.ToString(), out var year) ? year : 0,
            Score = int.TryParse(x["score"]?.ToString(), out var score) ? score : 0,
            Image = x["poster"]!.ToString(),
            Site = AnimeSites.AnimePahe
        }).ToList();
    }

    public async Task<List<AnimeInfo>> GetAiringAsync(
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var animes = new List<AnimeInfo>();

        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/api?m=airing&page={page}",
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(response))
            return animes;

        var data = JObject.Parse(response)["data"];
        if (data is null)
            return animes;

        return data.Select(x => new AnimeInfo()
        {
            Id = x["anime_session"]!.ToString(),
            Title = x["anime_title"]!.ToString(),
            Image = x["snapshot"]!.ToString(),
            Site = AnimeSites.AnimePahe
        }).ToList();
    }

    public async Task<AnimeInfo> GetAnimeInfoAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.ExecuteAsync($"{BaseUrl}/anime/{id}", cancellationToken);

        var document = Html.Parse(response);

        var anime = new AnimeInfo()
        {
            Id = id,
            Site = AnimeSites.AnimePahe
        };

        anime.Title = document.DocumentNode
            .SelectSingleNode(".//div[contains(@class, 'header-wrapper')]/header/div/h1/span")?
            .InnerText ?? "";

        anime.Image = document.DocumentNode
            .SelectSingleNode(".//header/div/div/div/a/img")!.Attributes["data-src"]!.Value;

        anime.Summary = document.DocumentNode
            .SelectSingleNode(".//div[contains(@class, 'anime-summary')]/div")?
            .InnerText ?? "";

        anime.Genres = document.DocumentNode
            .SelectNodes(".//div[contains(@class, 'anime-info')]/div/ul/li/a")
            .Select(el => new Genre(el.Attributes["title"].Value)).ToList();

        var list = document.DocumentNode
            .SelectNodes(".//div[contains(@class, 'anime-info')]/p").ToList();

        var typeNode = list.Find(x => x.ChildNodes
            .ElementAtOrDefault(0)?.InnerText?.ToLower().Contains("type") == true);

        var otherNamesCount = list.IndexOf(typeNode);

        anime.Type = typeNode?.SelectSingleNode(".//a")?.InnerText?.Trim() ?? "";

        var otherNameNodes = list.Take(otherNamesCount).ToList();

        anime.OtherNames = otherNameNodes.FirstOrDefault()?
            .ChildNodes.ElementAtOrDefault(1)?.InnerText?.Trim() ?? "";

        var releasedNode = list.Find(x => x.ChildNodes
            .ElementAtOrDefault(1)?.InnerText?.ToLower().Contains("aired") == true);

        anime.Released = releasedNode?
            .InnerText?.Split(new[] { "to" }, StringSplitOptions.None)[0]
            .Trim().Replace("Aired:", "").Replace("\n", "")
            .Replace("\r", "").Replace("\t", "") ?? "";

        var statusNode = list.Find(x => x.ChildNodes
            .ElementAtOrDefault(1)?.InnerText?.ToLower().Contains("status") == true);

        anime.Status = statusNode?.SelectSingleNode(".//a")?.InnerText?.Trim() ?? "";

        var seasonNode = list.Find(x => x.ChildNodes
            .ElementAtOrDefault(0)?.InnerText?.ToLower().Contains("season") == true);

        anime.Season = seasonNode?.SelectSingleNode(".//a")?.InnerText?.Trim() ?? "";

        return anime;
    }

    public async Task<List<Episode>> GetEpisodesAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var list = new List<Episode>();

        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/api?m=release&id={id}&sort=episode_asc&page=1",
            cancellationToken
        );

        var data = JObject.Parse(response);

        Func<JToken, Episode> epsSelector = (JToken el) =>
        {
            var link = $"{BaseUrl}/play/{id}/{el["session"]}";

            return new Episode()
            {
                //Description = el["description"]!.ToString(),
                //Id = el["id"]!.ToString(),
                //Id = el["session"]!.ToString(),
                Id = link,
                Number = Convert.ToInt32(el["episode"]!),
                Image = el["snapshot"]!.ToString(),
                Description = el["title"]!.ToString(),
                Link = link,
                Duration = (float)TimeSpan.Parse(el["duration"]!.ToString()).TotalMilliseconds
            };
        };

        list.AddRange(data["data"]!.Select(epsSelector));

        var lastPage = Convert.ToInt32(data["last_page"]);

        if (lastPage < 2)
            return list;

        // Start at index of 2 since we've already gotten the first page above.
        var functions = Enumerable.Range(2, lastPage - 1).Select(i =>
            (Func<Task<string>>)(async () => await _http.ExecuteAsync(
                $"{BaseUrl}/api?m=release&id={id}&sort=episode_asc&page={i}"
            )));

        var results = await TaskEx.Run(functions, 20);

        list.AddRange(results.SelectMany(response => JObject.Parse(response)["data"]!.Select(epsSelector)));

        return list;
    }

    public async Task<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default)
    {
        //var test = $"{BaseUrl}/play/api?m=links&id={episodeId}";

        var response = await _http.ExecuteAsync(
            //$"{BaseUrl}/api?m=links&id={episodeId}",
            episodeId, new Dictionary<string, string>()
            {
                { "Referer", BaseUrl }
            },
            cancellationToken
        );

        var document = Html.Parse(response);

        return document.GetElementbyId("pickDownload").SelectNodes(".//a")
            .Select(el =>
            {
                //var match = _videoServerRegex.Match(el.InnerText);
                //var matches = _videoServerRegex.Matches(el.InnerText).OfType<Match>().ToList();
                var match = _videoServerRegex.Match(el.InnerText);
                var groups = match.Groups.OfType<Group>();

                var subgroup = groups.ElementAtOrDefault(1)?.Value;
                var quality = groups.ElementAtOrDefault(2)?.Value;
                var mb = groups.ElementAtOrDefault(3)?.Value;
                var audio = groups.ElementAtOrDefault(4)?.Value;

                var audioName = !string.IsNullOrWhiteSpace(audio) ? $"{audio} " : "";

                return new VideoServer
                {
                    Name = $"{subgroup} {audioName}- {quality}p",
                    Embed = new FileUrl()
                    {
                        Url = el.Attributes["href"]!.Value,
                        Headers = new()
                        {
                            { "Referer", BaseUrl }
                        }
                    }
                };
            }).ToList();
    }

    public async Task<List<VideoSource>> GetVideosAsync(
        VideoServer server,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.IsWellFormedUriString(server.Embed.Url, UriKind.Absolute))
            return new();

        return await new Kwik(_httpClientProvider)
            .ExtractAsync(server.Embed.Url, cancellationToken);
    }
}