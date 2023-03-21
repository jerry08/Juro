using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models.Manga;
using Juro.Models.Manga.Mangadex;
using Juro.Utils.Extensions;
using Newtonsoft.Json.Linq;

namespace Juro.Providers.Manga;

public class Mangadex : MangaParser<MangadexResult, MangadexInfo>
{
    private readonly string _apiUrl = "https://api.mangadex.org";

    public override string Name { get; set; } = "Mangadex";

    public override string BaseUrl => "https://mangadex.org";

    public override string Logo => "https://pbs.twimg.com/profile_images/1391016345714757632/xbt_jW78_400x400.jpg";

    public Mangadex(HttpClient httpClient) : base(httpClient)
    {
    }

    public override async Task<List<MangadexResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default!)
    {
        return await SearchAsync(query, 1, 20, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="page">Page number. Default value is 1.</param>
    /// <param name="limit">Limit of results to return. (default: 20) (max: 100) (min: 1)</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<List<MangadexResult>> SearchAsync(
        string query,
        int page = 1,
        int limit = 20,
        CancellationToken cancellationToken = default!)
    {
        if (page <= 0) throw new Exception("Page number must be greater than 0");
        if (limit > 100) throw new Exception("Limit must be less than or equal to 100");
        if (limit * (page - 1) >= 10000) throw new Exception("not enough results");

        var url = $"{_apiUrl}/manga?limit={limit}&title={Uri.EscapeDataString(query)}&limit={limit}&offset={limit * (page - 1)}&order[relevance]=desc";
        var response = await _http.ExecuteAsync(url, cancellationToken);

        var data = JObject.Parse(response);
        if (data["result"]?.ToString() != "ok")
            return new();

        var list = data["data"]!.Select(manga => new MangadexResult()
        {
            Id = manga["id"]!.ToString(),
            Title = manga["attributes"]!["title"]!.ToList()[0].Value<JProperty>()!.Value.ToString(),
            AltTitles = manga["attributes"]!["altTitles"]?
                .Children<JObject>()
                .OrderByDescending(x => "en")
                .ThenBy(x => x.Properties().First().Name)
                .Select(x => x.Properties().First().Value!.ToString())
                .ToList() ?? new(),
            //Description = manga["attributes"]!["description"]!["en"]!.ToString(),
            Descriptions = manga["attributes"]!["description"]?.Select(x => new MangadexDescription()
            {
                Description = ((JProperty)x).Name,
                Language = ((JProperty)x).Value?.ToString()
            }).ToList() ?? new(),
            Status = manga["attributes"]!["status"]?.ToString().ToLower() switch
            {
                "completed" => MediaStatus.Completed,
                "ongoing" => MediaStatus.Ongoing,
                _ => MediaStatus.Unknown,
            },
            ReleaseDate = int.TryParse(manga["attributes"]!["year"]?.ToString(), out var year) ? year : null,
            ContentRating = manga["attributes"]!["contentRating"]!.ToString(),
            LastVolume = manga["attributes"]!["lastVolume"]!.ToString(),
            LastChapter = manga["attributes"]!["lastChapter"]!.ToString(),
        }).ToList();

        return list;
    }

    public override async Task<MangadexInfo> GetMangaInfoAsync(
        string mangaId,
        CancellationToken cancellationToken = default!)
    {
        var url = $"{_apiUrl}/manga/{mangaId}";
        var response = await _http.ExecuteAsync(url, cancellationToken);

        var data = JObject.Parse(response);
        if (data["result"]?.ToString() != "ok")
            return new();

        var mangaInfo = new MangadexInfo
        {
            Id = data["data"]!["id"]!.ToString(),
            Title = data["data"]!["attributes"]!["title"]!["en"]!.ToString(),
            AltTitles = data["data"]!["attributes"]!["altTitles"]!
                .Children<JObject>()
                .OrderByDescending(x => "en")
                .ThenBy(x => x.Properties().First().Name)
                .Select(x => x.Properties().First().Value!.ToString())
                .ToList(),
            Description = data["data"]!["attributes"]!["description"]!["en"]!.ToString(),
            Genres = data["data"]!["attributes"]!["tags"]!
                .Where(tag => tag["attributes"]!["group"]?.ToString() == "genre")
                .Select(tag => tag["attributes"]!["name"]!["en"]!.ToString())
                .ToList(),
            Themes = data["data"]!["attributes"]!["tags"]!
                .Where(tag => tag["attributes"]!["group"]?.ToString() == "theme")
                .Select(tag => tag["attributes"]!["name"]!["en"]!.ToString())
                .ToList(),
            Status = data["data"]!["attributes"]!["status"]!.ToString().ToLower() switch
            {
                "completed" => MediaStatus.Completed,
                "ongoing" => MediaStatus.Ongoing,
                _ => MediaStatus.Unknown,
            },
            ReleaseDate = Convert.ToInt32(data["data"]!["attributes"]!["year"]!)
        };

        var chapters = await GetAllChaptersAsync(mangaId, 0, cancellationToken);
        mangaInfo.Chapters.AddRange(chapters);

        var coverArtId = data["data"]!["relationships"]!
            .Where(x => x["type"]!.ToString() == "cover_art")
            .FirstOrDefault()?["id"]!.ToString();

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
    public override async Task<List<MangaChapterPage>> GetChapterPagesAsync(
        string chapterId,
        CancellationToken cancellationToken = default!)
    {
        var url = $"{_apiUrl}/at-home/server/{chapterId}";
        var response = await _http.ExecuteAsync(url, cancellationToken);

        var data = JObject.Parse(response);
        if (data["result"]?.ToString() != "ok")
            return new();

        var pages = data!["chapter"]!["data"]!.Select(id => new MangaChapterPage()
        {
            Image = $"{data!["baseUrl"]}/data/{data!["chapter"]!["hash"]}/{id}",
            Page = Convert.ToInt32(id.ToString().Split('-')[0])
        }).ToList();

        return pages;
    }

    public async Task<List<MangadexChapter>> GetAllChaptersAsync(
        string mangaId,
        int offset,
        CancellationToken cancellationToken = default!)
    {
        var list = new List<MangadexChapter>();

        while (true)
        {
            var url = $"{_apiUrl}/manga/{mangaId}/feed?offset={offset}&limit=96&order[volume]=desc&order[chapter]=desc&translatedLanguage[]=en";
            var response = await _http.ExecuteAsync(url, cancellationToken);

            var data = JObject.Parse(response);
            if (data["result"]?.ToString() != "ok")
                break;

            var remaining = Convert.ToInt32(data["total"]) - offset;
            if (remaining <= 0)
                break;

            offset += 96;

            list.AddRange(data["data"]!.Select(chapter => new MangadexChapter()
            {
                Id = chapter["id"]!.ToString(),
                Title = !string.IsNullOrEmpty(chapter["attributes"]!["title"]!.ToString())
                    ? chapter["attributes"]!["title"]!.ToString() : chapter["attributes"]!["chapter"]!.ToString(),
                Pages = Convert.ToInt32(chapter["attributes"]!["pages"]!),
            }));
        }

        return list;
    }

    public async Task<string> GetCoverImageAsync(
        string coverId,
        CancellationToken cancellationToken = default!)
    {
        var url = $"{_apiUrl}/cover/{coverId}";
        var response = await _http.ExecuteAsync(url, cancellationToken);

        var data = JObject.Parse(response);
        if (data["result"]?.ToString() != "ok")
            return string.Empty;

        return data["data"]!["attributes"]!["fileName"]!.ToString();
    }
}