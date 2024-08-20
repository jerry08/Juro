using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Juro.Core;
using Juro.Core.Models.Anime;
using Juro.Core.Models.Videos;
using Juro.Core.Providers;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;
using Juro.Core.Utils.Tasks;
using Juro.Extractors;

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with Aniwave.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="Aniwave"/>.
/// </remarks>
public class Aniwave(IHttpClientFactory httpClientFactory)
    : AnimeBaseProvider(httpClientFactory),
        IAnimeProvider,
        IPopularProvider,
        ILastUpdatedProvider
{
    private readonly HttpClient _http = httpClientFactory.CreateClient();
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public string Key => Name;

    public string Name => "Aniwave";

    public string Language => "en";

    public bool IsDubAvailableSeparately => false;

    public string BaseUrl => "https://aniwave.to";

    /// <summary>
    /// Initializes an instance of <see cref="Aniwave"/>.
    /// </summary>
    public Aniwave(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="Aniwave"/>.
    /// </summary>
    public Aniwave()
        : this(Http.ClientProvider) { }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        //var vrf = await EncodeVrfAsync(Uri.EscapeDataString(query), cancellationToken);

        //  var url = $"{BaseUrl}/filter?keyword={Uri.EscapeDataString(query).Replace("%20", "+")}";
        //var url = $"{BaseUrl}/ajax/search?keyword={Uri.EscapeDataString(query).Replace("%20", "+")}";
        var url =
            $"{BaseUrl}/ajax/anime/search?keyword={Uri.EscapeDataString(query).Replace("%20", "+")}";
        //url = $"{url}&sort=${filters.sort}&{vrf}&page={page}";
        //url = $"{url}&{vrf}";

        var response = await _http.ExecuteAsync(
            url,
            new Dictionary<string, string>() { ["Referer"] = BaseUrl },
            //new Dictionary<string, string>()
            //{
            //    ["Referer"] = BaseUrl,
            //    ["Cookie"] = "waf_jschallenge_c40d24eb070195c3=1701458622-2ab75ee0ecf1d26ea40ad78ad986ae2a",
            //    ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36"
            //},
            cancellationToken
        );

        return ParseAnimeSearchResponse(response);
    }

    /// <inheritdoc cref="SearchAsync"/>
    public async ValueTask<List<IAnimeInfo>> GetPopularAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/filter?sort=trending&page={page}",
            cancellationToken
        );

        return ParseAnimeResponse(response);
    }

    /// <summary>
    /// Gets anime in new season.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="cancellationToken"></param>
    public async ValueTask<List<IAnimeInfo>> GetLastUpdatedAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/filter?sort=recently_updated&page={page}",
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
        var nodes = document.DocumentNode.SelectNodes(
            ".//div[@id='list-items']/div[contains(@class, 'item')]"
        );

        foreach (var node in nodes)
        {
            var animeInfo = new AnimeInfo() { Site = AnimeSites.Aniwave };

            //animeInfo.Id = node.SelectSingleNode(
            //    ".//div/div[contains(@class, 'ani')]/a"
            //).Attributes["href"].Value?.Split('/')[2]!;

            animeInfo.Id = node.SelectSingleNode(".//div/div[contains(@class, 'ani')]/a")
                .Attributes["href"]
                .Value;

            animeInfo.Title = node.SelectSingleNode(
                ".//div/div[contains(@class, 'info')]/div[contains(@class, 'b1')]/a"
            ).InnerText;

            animeInfo.Image = node.SelectSingleNode(".//div/div[contains(@class, 'ani')]/a/img")
                .Attributes["src"]
                .Value;

            list.Add(animeInfo);
        }

        return list;
    }

    private List<IAnimeInfo> ParseAnimeSearchResponse(string? response)
    {
        var list = new List<IAnimeInfo>();

        if (string.IsNullOrWhiteSpace(response))
            return list;

        var data = JsonNode.Parse(response!)!;

        response = data["result"]?["html"]?.ToString();

        var document = Html.Parse(response!);
        var nodes = document.DocumentNode.SelectNodes("//a[contains(@class, 'item')]");

        foreach (var node in nodes)
        {
            var animeInfo = new AnimeInfo() { Site = AnimeSites.Aniwave };

            animeInfo.Id = node.GetAttributeValue("href", "");

            animeInfo.Title = node.SelectSingleNode(".//div[@class='name d-title']")
                .InnerText.Trim();

            animeInfo.Image = node.SelectSingleNode(".//img").GetAttributeValue("src", "");

            animeInfo.Released = node.SelectSingleNode(".//div[@class='meta']/span[last()]")
                .InnerText.Trim();

            animeInfo.Type = node.SelectSingleNode(".//div[@class='meta']/span[last()-1]")
                .InnerText.Trim();

            list.Add(animeInfo);
        }

        return list;
    }

    /// <inheritdoc />
    public async ValueTask<IAnimeInfo> GetAnimeInfoAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync($"{BaseUrl}{id}", cancellationToken);

        var document = Html.Parse(response);

        var anime = new AnimeInfo()
        {
            Id = id,
            Site = AnimeSites.Aniwave,
            Title = document
                .DocumentNode.SelectSingleNode(".//h1[contains(@class, 'title')]")
                .InnerText,
            Image = document
                .DocumentNode.SelectSingleNode(
                    ".//div[contains(@class, 'binfo')]/div[contains(@class, 'poster')]/span/img"
                )
                .Attributes["src"]
                .Value,
        };

        var jpAttr = document
            .DocumentNode.SelectSingleNode(".//h1[contains(@class, 'title')]")
            .Attributes["data-jp"];
        if (jpAttr is not null)
            anime.OtherNames = jpAttr.Value;

        anime.Summary =
            document.DocumentNode.SelectSingleNode(".//div[@class='content']").InnerText?.Trim()
            ?? "";

        var genresNode = document
            .DocumentNode.SelectNodes(".//div[contains(@class, 'meta')][1]/div")
            .FirstOrDefault(x => x.InnerText?.ToLower().Contains("genres") == true)
            ?.SelectNodes(".//span/a");
        if (genresNode is not null)
            anime.Genres.AddRange(genresNode.Select(x => new Genre(x.InnerText)));

        var airedNode = document
            .DocumentNode.SelectNodes(".//div[contains(@class, 'meta')][1]/div")
            .FirstOrDefault(x => x.InnerText?.ToLower().Contains("aired") == true)
            ?.SelectSingleNode(".//span");
        if (airedNode is not null)
            anime.Released = airedNode.InnerText.Trim();

        var typeNode = document
            .DocumentNode.SelectNodes(".//div[contains(@class, 'meta')][1]/div")
            .FirstOrDefault(x => x.InnerText?.ToLower().Contains("type") == true)
            ?.SelectSingleNode(".//span");
        if (typeNode is not null)
            anime.Type = typeNode.InnerText.Trim();

        var statusNode = document
            .DocumentNode.SelectNodes(".//div[contains(@class, 'meta')][1]/div")
            .FirstOrDefault(x => x.InnerText?.ToLower().Contains("status") == true)
            ?.SelectSingleNode(".//span");
        if (statusNode is not null)
            anime.Status = statusNode.InnerText.Trim();

        return anime;
    }

    /// <inheritdoc />
    public async ValueTask<List<Episode>> GetEpisodesAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var list = new List<Episode>();

        var response = await _http.ExecuteAsync($"{BaseUrl}{id}", cancellationToken);

        var document = Html.Parse(response);
        var dataId = document
            .DocumentNode.SelectNodes(".//div[@data-id]")!
            .FirstOrDefault()!
            .Attributes["data-id"]
            .Value;

        var vrf = await EncodeVrfAsync(dataId, cancellationToken);

        var response2 = await _http.ExecuteAsync(
            $"{BaseUrl}/ajax/episode/list/{dataId}?vrf={vrf}",
            new Dictionary<string, string>() { ["url"] = BaseUrl + id },
            cancellationToken
        );

        var html = JsonNode.Parse(response2)!["result"]!.ToString();
        document = Html.Parse(html);

        var nodes = document.DocumentNode.SelectNodes(
            ".//div[contains(@class, 'episodes')]/ul/li/a"
        );

        foreach (var node in nodes)
        {
            var possibleIds = node.Attributes["data-ids"]?.Value.Split(',')!;

            list.Add(
                new Episode()
                {
                    Id = possibleIds[0], // Sub
                    Name = node.SelectSingleNode(".//span")?.InnerText,
                    //Link = link,
                    Number = int.Parse(node.Attributes["data-num"]?.Value ?? "0"),
                }
            );
        }

        return list;
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    )
    {
        var vrf = await EncodeVrfAsync(episodeId, cancellationToken);

        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/ajax/server/list/{episodeId}?vrf={vrf}",
            cancellationToken
        );

        var html = JsonNode.Parse(response)!["result"]!.ToString();

        var document = Html.Parse(html);

        var list = new List<VideoServer>();

        var nodes = document.DocumentNode.SelectNodes(".//div[@class='type']/ul/li")?.ToList();
        if (nodes is null)
            return list;

        var functions = Enumerable
            .Range(0, nodes.Count)
            .Select(i =>
                (Func<Task<VideoServer>>)(
                    async () => await GetVideoServerAsync(nodes[i], cancellationToken)
                )
            );

        list.AddRange(await TaskEx.Run(functions, 10));

        return list;
    }

    private async ValueTask<VideoServer> GetVideoServerAsync(
        HtmlNode node,
        CancellationToken cancellationToken = default
    )
    {
        var serverId = node.Attributes["data-link-id"].Value;

        var vrf2 = await EncodeVrfAsync(serverId, cancellationToken);

        var response3 = await _http.ExecuteAsync(
            $"{BaseUrl}/ajax/server/{serverId}?vrf={vrf2}",
            cancellationToken
        );

        var encodedStreamUrl = JsonNode.Parse(response3)?["result"]?["url"]?.ToString();

        var realLink = await DecodeVrfAsync(encodedStreamUrl!, cancellationToken);

        return new()
        {
            Name = node.InnerText,
            Embed = new(realLink) { Headers = new() { ["Referer"] = BaseUrl } },
        };
    }

    public override IVideoExtractor? GetVideoExtractor(VideoServer server)
    {
        return server.Name.ToLower() switch
        {
            "vidstream" => new AniwaveExtractor(_httpClientFactory, "VidStream"),
            "mycloud" => new AniwaveExtractor(_httpClientFactory, "MyCloud"),
            "filemoon" => new FilemoonExtractor(_httpClientFactory),
            "streamtape" => new StreamTapeExtractor(_httpClientFactory),
            "mp4upload" => new Mp4uploadExtractor(_httpClientFactory),
            _ => null,
        };
    }

    /// <summary>
    /// Encodes a string by making an http request to <see href="https://9anime.eltik.net"/>.
    /// </summary>
    /// <param name="query">The string to encode.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An encoded string.</returns>
    public async ValueTask<string> EncodeVrfAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"https://9anime.eltik.net/vrf?query={query}&apikey=chayce",
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(response))
            return string.Empty;

        var data = JsonNode.Parse(response)!;

        var vrf = data["url"]?.ToString();

        if (!string.IsNullOrWhiteSpace(vrf))
            return vrf!;

        return string.Empty;
    }

    /// <summary>
    /// Decodes a string by making an http request to <see href="https://9anime.eltik.net"/>.
    /// </summary>
    /// <param name="query">The string to decode.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A decoded string.</returns>
    public async ValueTask<string> DecodeVrfAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"https://9anime.eltik.net/decrypt?query={query}&apikey=chayce",
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(response))
            return string.Empty;

        var data = JsonNode.Parse(response)!;

        var vrf = data["url"]?.ToString();

        if (!string.IsNullOrWhiteSpace(vrf))
            return vrf!;

        return string.Empty;
    }
}
