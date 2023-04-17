using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models.Manga;
using Juro.Utils;
using Juro.Utils.Extensions;

namespace Juro.Providers.Manga;

public class MangaPill : IMangaProvider
{
    private readonly HttpClient _http;

    public string Name { get; set; } = "MangaPill";

    public string BaseUrl => "https://mangapill.com";

    public string Logo => "";

    public MangaPill(Func<HttpClient> httpClientProvider)
    {
        _http = httpClientProvider();
    }

    public async Task<List<IMangaResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default!)
    {
        var response = await _http.ExecuteAsync($"{BaseUrl}/quick-search?q={Uri.EscapeUriString(query)}", cancellationToken);

        var document = Html.Parse(response);

        return document.DocumentNode
            .SelectNodes(".//a[contains(@class, 'bg-card')]")?
            .Select(el => (IMangaResult)new MangaResult()
            {
                Id = el.Attributes["href"].Value,
                Title = el.SelectSingleNode(".//div[@class='font-black']")?.InnerText,
                Image = el.SelectSingleNode(".//img")?.Attributes["src"]?.Value
            }).ToList() ?? new();
    }

    public async Task<IMangaInfo> GetMangaInfoAsync(
        string mangaId,
        CancellationToken cancellationToken = default!)
    {
        var url = BaseUrl + mangaId;
        var response = await _http.ExecuteAsync(url, cancellationToken);

        var document = Html.Parse(response);

        var mangaInfo = new MangaInfo
        {
            Id = mangaId
        };

        mangaInfo.Description = document.DocumentNode.SelectSingleNode(".//div[@class='flex flex-col']/div[2]/p")?.InnerText.Trim();
        mangaInfo.Genres = document.DocumentNode.SelectNodes(".//div[@class='flex flex-col']/div[4]/a")?
            .Select(el => el.InnerText).ToList() ?? new();

        var statusText = document.DocumentNode.SelectSingleNode(".//div[@class='flex flex-col']/div[3]/div[2]/div")?.InnerText.Trim();
        mangaInfo.Status = statusText switch
        {
            "finished" => MediaStatus.Completed,
            "publishing" => MediaStatus.Ongoing,
            _ => MediaStatus.Unknown,
        };

        mangaInfo.Chapters = document.DocumentNode.SelectNodes(".//div[@id='chapters']/div/a")?
            .Reverse()?.Select(el => (IMangaChapter)new MangaChapter()
            {
                Id = el.Attributes["href"].Value,
                Title = el.InnerText
            }).ToList() ?? new();

        return mangaInfo;
    }

    public async Task<List<IMangaChapterPage>> GetChapterPagesAsync(
        string chapterId,
        CancellationToken cancellationToken = default!)
    {
        var url = BaseUrl + chapterId;
        var response = await _http.ExecuteAsync(url, cancellationToken);

        var document = Html.Parse(response);

        var i = 1;

        return document.DocumentNode.SelectNodes(".//img[@class='js-page']")
            .Select(el => (IMangaChapterPage)new MangaChapterPage()
            {
                Image = el.Attributes["data-src"]!.Value,
                Page = i++
            }).ToList();
    }
}