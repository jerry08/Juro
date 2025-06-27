using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core;
using Juro.Core.Converters;
using Juro.Core.Models.Anime;
using Juro.Core.Models.Videos;
using Juro.Core.Providers;
using Juro.Core.Utils;

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with AnimeOwl.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="AnimeOwl"/>.
/// </remarks>
public class AnimeOwl(IHttpClientFactory httpClientFactory) : IAnimeProvider
{
    public bool IsDubAvailableSeparately => throw new NotImplementedException();

    public string Key => Name;

    public string Name => "AnimeOwl";

    public string Language => "en";

    public string BaseUrl => "https://animeowl.live";

    /// <summary>
    /// Initializes an instance of <see cref="AnimeOwl"/>.
    /// </summary>
    public AnimeOwl(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="AnimeOwl"/>.
    /// </summary>
    public AnimeOwl()
        : this(Http.ClientProvider) { }

    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        var test = await SearchAsync(1, Sort.Search, query);

        return [];
    }

    public enum Sort
    {
        Latest = 1,
        Search = 4,
    }

    public async Task<AnimesPage> SearchAsync(
        int page,
        Sort sort,
        string query = "",
        int? limit = 30
    )
    {
        var body = new
        {
            lang22 = 3,
            value = query,
            sortt = sort.ToString(),
            limit = limit,
            page = page - 1,
            selected = new
            {
                type = new List<string>(),
                sort = new List<string>(),
                year = new List<string>(),
                genre = new List<string>(),
                season = new List<string>(),
                status = new List<string>(),
                country = new List<string>(),
                language = new List<string>(),
            },
        };

        // Convert the body to JSON and create the HttpContent
        var jsonBody = JsonSerializer.Serialize(body);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var client = httpClientFactory.CreateClient();

        var response = await client.PostAsync($"{BaseUrl}/api/advance-search", content);
        response.EnsureSuccessStatusCode();

        var options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            //NumberHandling = JsonNumberHandling.AllowReadingFromString,
            //Converters = { new BoolConverter() },
        };
        var res = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SearchResponse>(res, options)!;

        // Calculate next page availability
        var nextPage = Math.Ceiling((float)result.Total / (limit ?? 30)) > page;

        // Map the result to a list of SAnime objects
        var animes = new List<SAnime>();
        foreach (var anime in result.Results)
        {
            var sAnime = new SAnime
            {
                UrlWithoutDomain = $"/anime/{anime.AnimeSlug}?mal={anime.MalId}",
                ThumbnailUrl = $"{BaseUrl}{anime.Image}",
                Title = anime.AnimeName ?? string.Empty,
            };
            animes.Add(sAnime);
        }

        // Return the AnimesPage object
        return new AnimesPage(animes, nextPage);
    }

    public ValueTask<IAnimeInfo> GetAnimeInfoAsync(
        string animeId,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public ValueTask<List<Episode>> GetEpisodesAsync(
        string animeId,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public ValueTask<List<VideoSource>> GetVideosAsync(
        VideoServer server,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }
}

public class SearchResponse
{
    public int Total { get; set; }
    public List<AnimeResult> Results { get; set; } = [];
}

public class AnimeResult
{
    [JsonPropertyName("anime_id")]
    public int AnimeId { get; set; }

    [JsonPropertyName("anime_name")]
    public string? AnimeName { get; set; }

    [JsonPropertyName("mal_id")]
    public int MalId { get; set; }

    //[JsonPropertyName("updated_at")]
    //public DateTimeOffset UpdatedAt { get; set; }

    [JsonPropertyName("jp_name")]
    public string? JpName { get; set; }

    [JsonPropertyName("anime_slug")]
    public string? AnimeSlug { get; set; }

    [JsonPropertyName("en_name")]
    public string? EnName { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("is_uncensored")]
    public bool IsUncensored { get; set; }

    [JsonPropertyName("webp")]
    public string? Webp { get; set; }

    [JsonConverter(typeof(IntegerConverter))]
    [JsonPropertyName("total_episodes")]
    public int TotalEpisodes { get; set; }

    [JsonConverter(typeof(IntegerConverter))]
    [JsonPropertyName("total_dub_episodes")]
    public string? TotalDubEpisodes { get; set; }
}

public class SAnime : AnimeInfo
{
    public string? UrlWithoutDomain { get; set; }
    public string? ThumbnailUrl { get; set; }
}

public class AnimesPage(List<SAnime> animes, bool nextPage)
{
    public List<SAnime> Animes { get; set; } = animes;
    public bool NextPage { get; set; } = nextPage;
}
