using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Clients;
using Juro.Extractors;
using Juro.Models;
using Juro.Models.Anime;
using Juro.Models.Videos;
using Juro.Utils;
using Juro.Utils.Extensions;
using Newtonsoft.Json.Linq;

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with 9anime.
/// </summary>
public class NineAnime : IAnimeProvider
{
    private readonly HttpClient _http;
    private readonly Func<HttpClient> _httpClientProvider;
    private readonly ConsumetClient _consumet;

    public string Name => "9anime";

    public bool IsDubAvailableSeparately => false;

    public string BaseUrl => "https://9anime.to";

    /// <summary>
    /// Initializes an instance of <see cref="NineAnime"/>.
    /// </summary>
    public NineAnime(Func<HttpClient> httpClientProvider)
    {
        _http = httpClientProvider();
        _httpClientProvider = httpClientProvider;
        _consumet = new(httpClientProvider);
    }

    /// <summary>
    /// Initializes an instance of <see cref="NineAnime"/>.
    /// </summary>
    public NineAnime() : this(Http.ClientProvider)
    {
    }

    /// <inheritdoc />
    public async Task<List<AnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var vrf = await _consumet.NineAnime.ExecuteActionAsync(
            Uri.EscapeDataString(query),
            "searchVrf",
            cancellationToken
        );

        var url = $"{BaseUrl}/filter?keyword={Uri.EscapeDataString(query).Replace("%20", "+")}";

        //url = $"{url}&sort=${filters.sort}&{vrf}&page={page}";
        url = $"{url}&{vrf}";

        var animes = new List<AnimeInfo>();

        var response = await _http.ExecuteAsync(
            url,
            new()
            {
                ["Referer"] = BaseUrl
            },
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(response))
            return animes;

        var document = Html.Parse(response);
        var nodes = document.DocumentNode
            .SelectNodes(".//div[@id='list-items']/div[contains(@class, 'item')]");

        var list = new List<AnimeInfo>();

        foreach (var node in nodes)
        {
            var animeInfo = new AnimeInfo()
            {
                Site = AnimeSites.NineAnime
            };

            //animeInfo.Id = node.SelectSingleNode(
            //    ".//div/div[contains(@class, 'ani')]/a"
            //).Attributes["href"].Value?.Split('/')[2]!;

            animeInfo.Id = node.SelectSingleNode(
                ".//div/div[contains(@class, 'ani')]/a"
            ).Attributes["href"].Value;

            animeInfo.Title = node.SelectSingleNode(
                ".//div/div[contains(@class, 'info')]/div[contains(@class, 'b1')]/a"
            ).InnerText;

            animeInfo.Image = node.SelectSingleNode(
                ".//div/div[contains(@class, 'ani')]/a/img"
            ).Attributes["src"].Value;

            list.Add(animeInfo);
        }

        return list;
    }

    /// <inheritdoc cref="SearchAsync"/>
    public async Task<List<AnimeInfo>> GetPopularAsync(
        int page,
        CancellationToken cancellationToken = default)
    {
        var animes = new List<AnimeInfo>();

        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/filter?sort=trending&page={page}",
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(response))
            return animes;

        var document = Html.Parse(response);
        var nodes = document.DocumentNode
            .SelectNodes(".//div[@id='list-items']/div[contains(@class, 'item')]");

        var list = new List<AnimeInfo>();

        foreach (var node in nodes)
        {
            var animeInfo = new AnimeInfo()
            {
                Site = AnimeSites.NineAnime
            };

            //animeInfo.Id = node.SelectSingleNode(
            //    ".//div/div[contains(@class, 'ani')]/a"
            //).Attributes["href"].Value?.Split('/')[2]!;

            animeInfo.Id = node.SelectSingleNode(
                ".//div/div[contains(@class, 'ani')]/a"
            ).Attributes["href"].Value;

            animeInfo.Title = node.SelectSingleNode(
                ".//div/div[contains(@class, 'info')]/div[contains(@class, 'b1')]/a"
            ).InnerText;

            animeInfo.Image = node.SelectSingleNode(
                ".//div/div[contains(@class, 'ani')]/a/img"
            ).Attributes["src"].Value;

            list.Add(animeInfo);
        }

        return list;
    }

    /// <summary>
    /// Gets anime in new season.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="cancellationToken"></param>
    public async Task<List<AnimeInfo>> GetLastUpdatedAsync(
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/filter?sort=recently_updated&page={page}",
            cancellationToken
        );

        return new();
    }

    /// <inheritdoc />
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

        return anime;
    }

    /// <inheritdoc />
    public async Task<List<Episode>> GetEpisodesAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var list = new List<Episode>();

        var response = await _http.ExecuteAsync(
            $"{BaseUrl}{id}",
            cancellationToken
        );

        var document = Html.Parse(response);
        var dataId = document.DocumentNode
            .SelectNodes(".//div[@data-id]")!.FirstOrDefault()!.Attributes["data-id"].Value;

        var vrf = await _consumet.NineAnime.ExecuteActionAsync(
            dataId,
            "vrf",
            cancellationToken
        );

        var response2 = await _http.ExecuteAsync(
            $"{BaseUrl}/ajax/episode/list/{dataId}?vrf={vrf}",
            new()
            {
                ["url"] = $"{BaseUrl}{id}"
            },
            cancellationToken
        );

        var html = JObject.Parse(response2)!["result"]!.ToString();
        document = Html.Parse(html);

        var nodes = document.DocumentNode
            .SelectNodes(".//div[contains(@class, 'episodes')]/ul/li/a");

        foreach (var node in nodes)
        {
            var possibleIds = node.Attributes["data-ids"]?.Value.Split(',')!;

            list.Add(new Episode()
            {
                Id = possibleIds[0], // Sub
                Name = node.SelectSingleNode(".//span").InnerText,
                //Link = link,
                Number = int.Parse(node.Attributes["data-num"]?.Value ?? "0")
            });
        }

        return list;
    }

    /// <inheritdoc />
    public async Task<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default)
    {
        var vrf = await _consumet.NineAnime.ExecuteActionAsync(
            episodeId,
            "vrf",
            cancellationToken
        );

        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/ajax/server/list/{episodeId}?vrf={vrf}",
            cancellationToken
        );

        var html = JObject.Parse(response)["result"]!.ToString();

        var document = Html.Parse(html);

        var list = new List<VideoServer>();

        foreach (var node in document.DocumentNode.SelectNodes(".//div[@class='type']/ul/li"))
        {
            var serverId = node.Attributes["data-link-id"].Value;

            list.Add(new()
            {
                Name = node.InnerText,
                Embed = new(serverId)
            });
        }

        return list;
    }

    public IVideoExtractor? GetVideoExtractor(VideoServer server)
    {
        return server.Name.ToLower() switch
        {
            "vidstream" => new VizCloud(_httpClientProvider, "vizcloud"),
            "mycloud" => new VizCloud(_httpClientProvider, "mcloud"),
            "filemoon" => new Filemoon(_httpClientProvider),
            "streamtape" => new StreamTape(_httpClientProvider),
            "mp4upload" => new Mp4upload(_httpClientProvider),
            _ => null
        };
    }

    /// <inheritdoc />
    public async Task<List<VideoSource>> GetVideosAsync(
        VideoServer server,
        CancellationToken cancellationToken = default)
    {
        var vrf = await _consumet.NineAnime.ExecuteActionAsync(
            server.Embed.Url,
            "rawVrf",
            cancellationToken
        );

        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/ajax/server/{server.Embed.Url}?vrf={vrf}",
            cancellationToken
        );

        var encryptedUrl = JObject.Parse(response)["result"]!["url"]!.ToString();

        server.Embed.Url = await _consumet.NineAnime.ExecuteActionAsync(
            encryptedUrl,
            "decrypt",
            cancellationToken
        );

        if (!Uri.IsWellFormedUriString(server.Embed.Url, UriKind.Absolute))
            return new();

        var extractor = GetVideoExtractor(server);
        if (extractor is null)
            return new();

        return await extractor.ExtractAsync(server.Embed.Url, cancellationToken);
    }
}