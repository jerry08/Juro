using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core;
using Juro.Core.Models.Manga;
using Juro.Core.Models.Manga.Mangadex;
using Juro.Core.Providers;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;

namespace Juro.Providers.Manga;

/// <summary>
/// Client for interacting with Mangadex.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="Mangadex"/>.
/// </remarks>
public class Mangadex(IHttpClientFactory httpClientFactory) : IMangaProvider
{
    private readonly HttpClient _http = httpClientFactory.CreateClient();
    private readonly string _apiUrl = "https://api.mangadex.org";

    public string Key => Name;

    public string Name { get; set; } = "Mangadex";

    public string Language => "en";

    public string BaseUrl => "https://mangadex.org";

    public string Logo =>
        "https://pbs.twimg.com/profile_images/1391016345714757632/xbt_jW78_400x400.jpg";

    /// <summary>
    /// Initializes an instance of <see cref="Mangadex"/>.
    /// </summary>
    public Mangadex(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="Mangadex"/>.
    /// </summary>
    public Mangadex()
        : this(Http.ClientProvider) { }

    /// <summary>
    /// Search for manga.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="List{IMangaResult}"/> of <see cref="IMangaResult"/>s from <see cref="MangadexResult"/>s.</returns>
    public async ValueTask<List<IMangaResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken
    )
    {
        return await SearchAsync(query, 1, 20, cancellationToken);
    }

    /// <summary>
    /// Search for manga.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="page">Page number. Default value is 1.</param>
    /// <param name="limit">Limit of results to return. (default: 20) (max: 100) (min: 1)</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="List{IMangaResult}"/> of <see cref="IMangaResult"/>s from <see cref="MangadexResult"/>s.</returns>
    /// <exception cref="Exception"></exception>
    public async ValueTask<List<IMangaResult>> SearchAsync(
        string query,
        int page = 1,
        int limit = 20,
        CancellationToken cancellationToken = default!
    )
    {
        if (page <= 0)
            throw new Exception("Page number must be greater than 0");

        if (limit > 100)
            throw new Exception("Limit must be less than or equal to 100");

        if (limit * (page - 1) >= 10000)
            throw new Exception("not enough results");

        var url =
            $"{_apiUrl}/manga?limit={limit}&title={Uri.EscapeDataString(query)}&limit={limit}&offset={limit * (page - 1)}&order[relevance]=desc";
        var response = await _http.ExecuteAsync(url, cancellationToken);

        var data = JsonNode.Parse(response);
        if (data?["result"]?.ToString() != "ok")
            return [];

        var list = new List<IMangaResult>();

        var results = data["data"]!
            .AsArray()
            .Select(manga => new MangadexResult()
            {
                Id = manga!["id"]!.ToString(),
                Title = manga["attributes"]!["title"]!
                    .AsObject()
                    .Select(x => x.Value)
                    .FirstOrDefault()
                    ?.ToString(),
                AltTitles =
                    manga["attributes"]!
                        ["altTitles"]
                        ?.AsArray()
                        .Select(x => x!.AsObject().FirstOrDefault())
                        .OrderByDescending(x => "en")
                        .ThenBy(x => x.Key)
                        .Select(x => x.Value?.ToString() ?? "")
                        .ToList()
                    ?? [],
                Descriptions =
                    manga["attributes"]!["description"]!
                        .AsObject()
                        .Select(x => new MangadexDescription()
                        {
                            Description = x.Key,
                            Language = x.Value?.ToString(),
                        })
                        .ToList()
                    ?? [],
                Status = manga["attributes"]!["status"]?.ToString().ToLower() switch
                {
                    "completed" => MediaStatus.Completed,
                    "ongoing" => MediaStatus.Ongoing,
                    _ => MediaStatus.Unknown,
                },
                ReleaseDate = int.TryParse(manga["attributes"]!["year"]?.ToString(), out var year)
                    ? year
                    : null,
                ContentRating = manga["attributes"]!["contentRating"]!.ToString(),
                LastVolume = manga["attributes"]!["lastVolume"]!.ToString(),
                LastChapter = manga["attributes"]!["lastChapter"]!.ToString(),
            })
            .ToList();

        list.AddRange(results);

        return list;
    }

    /// <summary>
    /// Gets the manga info by Id.
    /// </summary>
    /// <param name="mangaId">The Id of the manga</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An interface of type <see cref="IMangaResult"/> from an instance of <see cref="MangadexInfo"/>.</returns>
    public async ValueTask<IMangaInfo> GetMangaInfoAsync(
        string mangaId,
        CancellationToken cancellationToken = default!
    )
    {
        var url = $"{_apiUrl}/manga/{mangaId}";
        var response = await _http.ExecuteAsync(url, cancellationToken);

        var data = JsonNode.Parse(response);
        if (data?["result"]?.ToString() != "ok")
            return new MangadexInfo();

        var mangaInfo = new MangadexInfo
        {
            Id = data["data"]!["id"]!.ToString(),
            Title = data["data"]!["attributes"]!["title"]!
                .AsObject()
                .Select(x => x.Value)
                .FirstOrDefault()
                ?.ToString(),
            AltTitles =
                data["data"]!["attributes"]!
                    ["altTitles"]
                    ?.AsArray()
                    .Select(x => x!.AsObject().FirstOrDefault())
                    .OrderByDescending(x => "en")
                    .ThenBy(x => x.Key)
                    .Select(x => x.Value?.ToString() ?? "")
                    .ToList()
                ?? [],
            Description = data["data"]!["attributes"]!["description"]!["en"]!.ToString(),
            Genres = data["data"]!["attributes"]!["tags"]!
                .AsArray()
                .Where(tag => tag!["attributes"]!["group"]?.ToString() == "genre")
                .Select(tag => tag!["attributes"]!["name"]!["en"]!.ToString())
                .ToList(),
            Themes = data["data"]!["attributes"]!["tags"]!
                .AsArray()
                .Where(tag => tag!["attributes"]!["group"]?.ToString() == "theme")
                .Select(tag => tag!["attributes"]!["name"]!["en"]!.ToString())
                .ToList(),
            Status = data["data"]!["attributes"]!["status"]!.ToString().ToLower() switch
            {
                "completed" => MediaStatus.Completed,
                "ongoing" => MediaStatus.Ongoing,
                _ => MediaStatus.Unknown,
            },
            ReleaseDate = Convert.ToInt32(data["data"]!["attributes"]!["year"]!.ToString()),
        };

        var chapters = await GetAllChaptersAsync(mangaId, 0, cancellationToken);
        mangaInfo.Chapters.AddRange(chapters);

        var coverArtId = data["data"]!["relationships"]!
            .AsArray()
            .FirstOrDefault(x => x!["type"]!.ToString() == "cover_art")
            ?["id"]!.ToString();

        if (coverArtId is not null)
        {
            var coverArt = await GetCoverImageAsync(coverArtId, cancellationToken);
            mangaInfo.Image = $"{BaseUrl}/covers/{mangaInfo.Id}/{coverArt}";
        }

        return mangaInfo;
    }

    /// <summary>
    /// Currently only supports english.
    /// </summary>
    public async ValueTask<List<IMangaChapterPage>> GetChapterPagesAsync(
        string chapterId,
        CancellationToken cancellationToken = default!
    )
    {
        var url = $"{_apiUrl}/at-home/server/{chapterId}";
        var response = await _http.ExecuteAsync(url, cancellationToken);

        var data = JsonNode.Parse(response);
        if (data?["result"]?.ToString() != "ok")
            return [];

        var list = new List<IMangaChapterPage>();

        var pages = data!["chapter"]!["data"]!
            .AsArray()
            .Select(id => new MangaChapterPage()
            {
                Image = $"{data!["baseUrl"]}/data/{data!["chapter"]!["hash"]}/{id}",
                Page = Convert.ToInt32(id!.ToString().Split('-')[0]),
            })
            .ToList();

        list.AddRange(pages);

        return list;
    }

    /// <summary>
    /// Gets all chapters from Mangadex Id.
    /// </summary>
    public async ValueTask<List<MangadexChapter>> GetAllChaptersAsync(
        string mangaId,
        int offset,
        CancellationToken cancellationToken = default!
    )
    {
        var list = new List<MangadexChapter>();

        while (true)
        {
            var url =
                $"{_apiUrl}/manga/{mangaId}/feed?offset={offset}&limit=96&order[volume]=desc&order[chapter]=desc&translatedLanguage[]=en";
            var response = await _http.ExecuteAsync(url, cancellationToken);

            var data = JsonNode.Parse(response);
            if (data?["result"]?.ToString() != "ok")
                break;

            var remaining = Convert.ToInt32(data["total"]!.ToString()) - offset;
            if (remaining <= 0)
                break;

            offset += 96;

            var count = 1;

            list.AddRange(
                data["data"]!
                    .AsArray()
                    .Reverse()
                    .Select(item =>
                    {
                        count++;

                        var title = item?["attributes"]?["title"]?.ToString();
                        var chapter = item?["attributes"]?["chapter"]?.ToString();

                        return new MangadexChapter()
                        {
                            Id = item!["id"]!.ToString(),
                            Title = !string.IsNullOrWhiteSpace(title) ? title : chapter,
                            Number = int.TryParse(chapter, out var num) ? num : count,
                            Pages = Convert.ToInt32(item["attributes"]!["pages"]!.ToString()),
                        };
                    })
            );
        }

        return list;
    }

    /// <summary>
    /// Gets cover image by cover Id.
    /// </summary>
    public async ValueTask<string> GetCoverImageAsync(
        string coverId,
        CancellationToken cancellationToken = default!
    )
    {
        var url = $"{_apiUrl}/cover/{coverId}";
        var response = await _http.ExecuteAsync(url, cancellationToken);

        var data = JsonNode.Parse(response);
        if (data?["result"]?.ToString() != "ok")
            return string.Empty;

        return data["data"]!["attributes"]!["fileName"]!.ToString();
    }
}
