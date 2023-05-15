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

/// <summary>
/// Client for interacting with MangaKatana.
/// </summary>
public class MangaKatana : IMangaProvider
{
    private readonly HttpClient _http;

    /// <inheritdoc />
    public string Name { get; set; } = "MangaKatana";

    /// <inheritdoc />
    public string BaseUrl => "https://mangakatana.com";

    /// <inheritdoc />
    public string Logo => "";

    /// <summary>
    /// Initializes an instance of <see cref="MangaKatana"/>.
    /// </summary>
    public MangaKatana(Func<HttpClient> httpClientProvider)
    {
        _http = httpClientProvider();
    }

    /// <summary>
    /// Initializes an instance of <see cref="MangaKatana"/>.
    /// </summary>
    public MangaKatana() : this(Http.ClientProvider)
    {
    }

    /// <inheritdoc />
    public async ValueTask<List<IMangaResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default!)
    {
        var url = $"{BaseUrl}/?search={Uri.EscapeDataString(query)}";
        var response = await _http.ExecuteAsync(url, cancellationToken);

        var document = Html.Parse(response);

        var list = new List<IMangaResult>();

        var bookListEl = document.GetElementbyId("book_list");
        if (bookListEl is not null)
        {
            var results = bookListEl.SelectNodes(".//div[contains(@class, 'item')]")?
                .Select(el => (IMangaResult)new MangaResult()
                {
                    Id = el.SelectSingleNode(".//a").Attributes["href"].Value,
                    Title = el.SelectSingleNode(".//img")?.Attributes["alt"]?.Value,
                    Image = el.SelectSingleNode(".//img")?.Attributes["src"]?.Value
                });

            if (results is not null)
                list.AddRange(results);
        }

        var singleBookEl = document.GetElementbyId("single_book");
        if (singleBookEl is not null)
        {
            var result = new MangaResult()
            {
                Title = singleBookEl.SelectSingleNode(".//img")?.Attributes["src"]?.Value,
                Image = singleBookEl.SelectSingleNode(".//div[@class='info']/h1[@class='heading']")?.InnerText
            };

            var i = 0;

            var chapters = document.DocumentNode
                .SelectNodes(".//div[@class='chapters']//div[@class='chapter']//a")
                .Select(el => new MangaChapterPage()
                {
                    Image = el.Attributes["href"]!.Value,
                    Title = el.InnerText,
                    Page = i++
                }).Reverse().ToList();

            var imgSplit = chapters.FirstOrDefault()!.Image.Split('/');

            result.Id = string.Join("/" , imgSplit.Take(imgSplit.Length - 1));

            //result.Chapters = chapters;

            list.Add(result);
        }

        return list;
    }

    /// <inheritdoc />
    public async ValueTask<IMangaInfo> GetMangaInfoAsync(
        string mangaId,
        CancellationToken cancellationToken = default!)
    {
        //var url = BaseUrl + mangaId;
        var url = mangaId;
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

    /// <inheritdoc />
    public async ValueTask<List<IMangaChapterPage>> GetChapterPagesAsync(
        string chapterId,
        CancellationToken cancellationToken = default!)
    {
        var url = BaseUrl + chapterId;
        var response = await _http.ExecuteAsync(url, cancellationToken);

        var document = Html.Parse(response);

        var i = 1;

        var list = new List<IMangaChapterPage>();

        list.AddRange(
            document.DocumentNode.SelectNodes(".//img[@class='js-page']")
                .Select(el => new MangaChapterPage()
                {
                    Image = el.Attributes["data-src"]!.Value,
                    Page = i++
                })
        );

        return list;
    }
}