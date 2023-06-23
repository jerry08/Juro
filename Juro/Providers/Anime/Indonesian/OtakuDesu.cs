﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models;
using Juro.Models.Anime;
using Juro.Models.Anime.Indonesian;
using Juro.Models.Videos;
using Juro.Utils;
using Juro.Utils.Extensions;

namespace Juro.Providers.Anime.Indonesian;

public class OtakuDesu : IAnimeProvider
{
    private readonly HttpClient _http;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <inheritdoc />
    public string Name => "OtakuDesu";

    /// <inheritdoc />
    public string Language => "id";

    public bool IsDubAvailableSeparately => false;

    /// <inheritdoc />
    public string BaseUrl => "https://otakudesu.lol";

    /// <summary>
    /// Initializes an instance of <see cref="OtakuDesu"/>.
    /// </summary>
    public OtakuDesu(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient();
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Initializes an instance of <see cref="OtakuDesu"/>.
    /// </summary>
    public OtakuDesu(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider))
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="OtakuDesu"/>.
    /// </summary>
    public OtakuDesu() : this(Http.ClientProvider)
    {
    }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/?s={Uri.EscapeDataString(query)}&post_type=anime",
            cancellationToken
        );

        return ParseAnimeResponse(response);
    }

    /// <summary>
    /// Searches for anime by specified genre.
    /// </summary>
    /// <param name="id">The name or url of the genre.</param>
    public async ValueTask<List<IAnimeInfo>> SearchByGenreAsync(
        string id,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var list = new List<IAnimeInfo>();

        var genre = Genres.Find(x => x.Url?.ToLower() == id.ToLower()
            || x.Name?.ToLower() == id.ToLower());

        if (genre is null)
            return list;

        var response = await _http.ExecuteAsync(
            $"{genre.Url}/page/{page}",
            cancellationToken
        );

        return ParseAnimeResponse(response);
    }

    private List<IAnimeInfo> ParseAnimeResponse(string? response)
    {
        var list = new List<IAnimeInfo>();

        if (string.IsNullOrWhiteSpace(response))
            return list;

        var document = Html.Parse(response!);

        var nodes = document.DocumentNode.SelectNodes(".//ul[@class='chivsrc']/li");

        for (var i = 0; i < nodes.Count; i++)
        {
            var anime = new OtakuDesuAnimeInfo()
            {
                Site = AnimeSites.OtakuDesu
            };

            var nameNode = nodes[i].SelectSingleNode(".//h2/a");
            if (nameNode is not null)
            {
                anime.Id = nameNode.Attributes["href"].Value;
                anime.Link = nameNode.Attributes["href"].Value;
                anime.Title = nameNode.InnerText;
            }
            else
            {
                continue;
            }

            var imgNode = nodes[i].SelectSingleNode(".//img");
            if (imgNode is not null)
                anime.Image = imgNode.Attributes["src"].Value;

            var genresNode = nodes[i].SelectNodes(".//div[@class='set']")
                .FirstOrDefault();
            if (genresNode is not null)
            {
                //var genres = genresNode.InnerText?.Split(':').Skip(1).ToArray();
                var genres = genresNode.InnerText?.Split(':').LastOrDefault()?.Split(',');
                anime.Genres = genres?.Select(x => new Genre(x.Trim())).ToList() ?? new();
            }

            var statusNode = nodes[i].SelectNodes(".//div[@class='set']")
                .ElementAtOrDefault(1);
            if (statusNode is not null)
                anime.Status = statusNode.InnerText?.Split(':').LastOrDefault()?.Trim();

            var ratingNode = nodes[i].SelectNodes(".//div[@class='set']")
                .ElementAtOrDefault(2);
            if (ratingNode is not null)
            {
                var ratingStr = ratingNode.InnerText?.Split(':').LastOrDefault()?.Trim();
                if (float.TryParse(ratingStr, out var rating))
                    anime.Rating = rating;
            }

            list.Add(anime);
        }

        return list;
    }

    /// <inheritdoc />
    public ValueTask<IAnimeInfo> GetAnimeInfoAsync(string animeId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async ValueTask<List<Episode>> GetEpisodesAsync(
        string animeId,
        CancellationToken cancellationToken = default)
    {
        var list = new List<Episode>();

        var response = await _http.ExecuteAsync(animeId, cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
            return list;

        var document = Html.Parse(response!);

        //var nodes = document.DocumentNode.SelectNodes(".//div[@class='episodelist']//a");
        //var nodes = document.DocumentNode.SelectNodes(".//div[@id='venkonten']//div[@class='venser']/div[8]/ul/li");
        var nodes = document.DocumentNode.SelectNodes(".//div[@id='venkonten']//div[@class='venser']/div[8]/ul/li");

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i].SelectSingleNode(".//a");

            var link = node.Attributes["href"].Value;

            list.Add(new()
            {
                Id = link,
                Number = i + 1,
                Name = node.InnerText,
                Link = link,
            });
        }

        return list;
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default)
    {
        //return await Task.FromResult(new List<VideoServer>()
        //{
        //    new(episodeId)
        //});

        var list = new List<VideoServer>()
        {
            new("Mirror", episodeId)
        };

        var response = await _http.ExecuteAsync(episodeId, cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
            return list;

        var document = Html.Parse(response!);

        list.AddRange(document.DocumentNode
            .SelectNodes(".//div[@class='download']/ul/li/a")
            .GroupBy(x => x.InnerText).Select(x => x.FirstOrDefault())
            .Where(x => x is not null)
            .Select(x => new VideoServer(x!.InnerText, episodeId))
        );

        return list;
    }

    public async ValueTask<List<VideoSource>> GetAllDownloadSourcesAsync(
        string episodeId,
        CancellationToken cancellationToken = default)
    {
        var list = new List<VideoSource>();

        var response = await _http.ExecuteAsync(episodeId, cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
            return list;

        var document = Html.Parse(response!);

        // Download nodes
        var nodes = document.DocumentNode.SelectNodes(".//div[@class='download']/ul/li/a")
            .Select(x => new VideoSource()
            {
                ExtraNote = x.InnerText, // Video server name
                Resolution = x.ParentNode.FirstChild.InnerText,
                Size = float.TryParse(x.ParentNode.LastChild.InnerText?.ToLower().Replace("mb", ""), out var size)
                    ? (long)(size * 1024.0)
                    : 0,
                VideoUrl = x.Attributes["href"].Value,
            });

        list.AddRange(nodes);

        return list;
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoSource>> GetVideosAsync(
        VideoServer server,
        CancellationToken cancellationToken = default)
    {
        var list = new List<VideoSource>();

        var response = await _http.ExecuteAsync(server.Embed.Url, cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
            return list;

        var document = Html.Parse(response!);

        // Download nodes
        var downloads = await GetAllDownloadSourcesAsync(server.Embed.Url, cancellationToken);

        list.AddRange(downloads.Where(x => x.ExtraNote == server.Name));

        if (list.Count > 0)
            return list;

        // Mirror nodes
        var script = document.DocumentNode.Descendants()
            .FirstOrDefault(x => x.Name == "script" && x.InnerText?.Contains("action:") == true)
            ?.InnerText;

        if (string.IsNullOrWhiteSpace(script))
            return list;

        var nonceAction = script!.SubstringAfter("{action:\"").SubstringBefore(@"""");
        var action = script!.SubstringAfter("action:\"").SubstringBefore(@"""");

        var nonce = await GetNonceAsync(nonceAction, cancellationToken);
        if (string.IsNullOrWhiteSpace(nonce))
            return list;

        var videoListNodes = document.DocumentNode.SelectNodes(".//div[@class='mirrorstream']/ul/li/a");

        for (var i = 0; i < videoListNodes.Count; i++)
        {
            var dataContent = videoListNodes[i].Attributes["data-content"].Value;

            var decodedData = dataContent.DecodeBase64();

            var jNode = JsonNode.Parse(decodedData)!;
            var id = jNode["id"];
            var mirror = jNode["i"];
            var quality = jNode["q"];
        }

        return list;
    }

    private async ValueTask<string?> GetNonceAsync(
        string action,
        CancellationToken cancellationToken = default)
    {
        var formContent = new FormUrlEncodedContent(new KeyValuePair<string?, string?>[]
        {
            new("action", action)
        });

        var response = await _http.PostAsync(
            $"{BaseUrl}/wp-admin/admin-ajax.php",
            formContent,
            cancellationToken);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonNode.Parse(content)?["data"]?.ToString();
    }

    public List<Genre> Genres => new()
    {
        new("Action", $"{BaseUrl}/genres/action"),
        new("Adventure", $"{BaseUrl}/genres/adventure"),
        new("Comedy", $"{BaseUrl}/genres/comedy"),
        new("Demons", $"{BaseUrl}/genres/demons"),
        new("Drama", $"{BaseUrl}/genres/drama"),
        new("Ecchi", $"{BaseUrl}/genres/ecchi"),
        new("Fantasy", $"{BaseUrl}/genres/fantasy"),
        new("Game", $"{BaseUrl}/genres/game"),
        new("Harem", $"{BaseUrl}/genres/harem"),
        new("Historical", $"{BaseUrl}/genres/historical"),
        new("Horror", $"{BaseUrl}/genres/horror"),
        new("Josei", $"{BaseUrl}/genres/josei"),
        new("Magic", $"{BaseUrl}/genres/magic"),
        new("Martial Arts", $"{BaseUrl}/genres/martial-arts"),
        new("Mecha", $"{BaseUrl}/genres/mecha"),
        new("Military", $"{BaseUrl}/genres/military"),
        new("Music", $"{BaseUrl}/genres/music"),
        new("Mystery", $"{BaseUrl}/genres/mystery"),
        new("Psychological", $"{BaseUrl}/genres/psychological"),
        new("Parody", $"{BaseUrl}/genres/parody"),
        new("Police", $"{BaseUrl}/genres/police"),
        new("Romance", $"{BaseUrl}/genres/romance"),
        new("Samurai", $"{BaseUrl}/genres/samurai"),
        new("School", $"{BaseUrl}/genres/school"),
        new("Sci-Fi", $"{BaseUrl}/genres/sci-fi"),
        new("Seinen", $"{BaseUrl}/genres/seinen"),
        new("Shoujo", $"{BaseUrl}/genres/shoujo"),
        new("Shoujo Ai", $"{BaseUrl}/genres/shoujo-ai"),
        new("Shounen", $"{BaseUrl}/genres/shounen"),
        new("Slice of Life", $"{BaseUrl}/genres/slice-of-life"),
        new("Sports", $"{BaseUrl}/genres/sports"),
        new("Space", $"{BaseUrl}/genres/space"),
        new("Super Power", $"{BaseUrl}/genres/super-power"),
        new("Supernatural", $"{BaseUrl}/genres/supernatural"),
        new("Thriller", $"{BaseUrl}/genres/thriller"),
        new("Vampire", $"{BaseUrl}/genres/vampire")
    };
}