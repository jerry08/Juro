using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core;
using Juro.Core.Models.Manga;
using Juro.Core.Providers;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;

namespace Juro.Providers.Manga;

/// <summary>
/// Client for interacting with MangaKatana.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="MangaKatana"/>.
/// </remarks>
public class MangaKatana(IHttpClientFactory httpClientFactory) : IMangaProvider
{
    private readonly HttpClient _http = httpClientFactory.CreateClient();

    public string Key => Name;

    /// <inheritdoc />
    public string Name { get; set; } = "MangaKatana";

    /// <inheritdoc />
    public string Language => "en";

    /// <inheritdoc />
    public string BaseUrl => "https://mangakatana.com";

    /// <inheritdoc />
    public string Logo => "";

    /// <summary>
    /// Initializes an instance of <see cref="MangaKatana"/>.
    /// </summary>
    public MangaKatana(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="MangaKatana"/>.
    /// </summary>
    public MangaKatana()
        : this(Http.ClientProvider) { }

    /// <inheritdoc />
    public async ValueTask<List<IMangaResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default!
    )
    {
        var url = $"{BaseUrl}/?search={Uri.EscapeDataString(query)}";
        var response = await _http.ExecuteAsync(url, cancellationToken);

        var document = Html.Parse(response);

        var list = new List<IMangaResult>();

        var bookListEl = document.GetElementbyId("book_list");
        if (bookListEl is not null)
        {
            var results = bookListEl
                .SelectNodes(".//div[contains(@class, 'item')]")
                ?.Select(el =>
                    (IMangaResult)
                        new MangaResult()
                        {
                            Id = el.SelectSingleNode(".//a").Attributes["href"].Value,
                            Title = el.SelectSingleNode(".//img")?.Attributes["alt"]?.Value,
                            Image = el.SelectSingleNode(".//img")?.Attributes["src"]?.Value
                        }
                );

            if (results is not null)
                list.AddRange(results);
        }

        var singleBookEl = document.GetElementbyId("single_book");
        if (singleBookEl is not null)
        {
            var result = new MangaResult()
            {
                Title = singleBookEl.SelectSingleNode(".//img")?.Attributes["src"]?.Value,
                Image = singleBookEl
                    .SelectSingleNode(".//div[@class='info']/h1[@class='heading']")
                    ?.InnerText
            };

            var i = 0;

            var chapters = document
                .DocumentNode.SelectNodes(".//div[@class='chapters']//div[@class='chapter']//a")
                .Select(el => new MangaChapterPage()
                {
                    Image = el.Attributes["href"]!.Value,
                    Title = el.InnerText,
                    Page = i++
                })
                .Reverse()
                .ToList();

            var imgSplit = chapters.FirstOrDefault()!.Image.Split('/');

            result.Id = string.Join("/", imgSplit.Take(imgSplit.Length - 1));

            //result.Chapters = chapters;

            list.Add(result);
        }

        return list;
    }

    /// <inheritdoc />
    public async ValueTask<IMangaInfo> GetMangaInfoAsync(
        string mangaId,
        CancellationToken cancellationToken = default!
    )
    {
        //var url = BaseUrl + mangaId;
        var url = mangaId;
        var response = await _http.ExecuteAsync(url, cancellationToken);

        var document = Html.Parse(response);

        var mangaInfo = new MangaInfo { Id = mangaId };

        mangaInfo.Description = document
            .DocumentNode.SelectSingleNode(".//div[@class='summary']/p")
            ?.InnerText.Trim();
        mangaInfo.Genres =
            document
                .DocumentNode.SelectNodes(".//div[@class='genres']/a")
                ?.Select(el => el.InnerText)
                .ToList() ?? [];

        var statusText = document
            .DocumentNode.SelectSingleNode(".//ul[@class='meta d-table']/li[4]/div[2]")
            ?.InnerText.Trim();
        mangaInfo.Status = statusText switch
        {
            "finished" or "completed" => MediaStatus.Completed,
            "publishing" => MediaStatus.Ongoing,
            _ => MediaStatus.Unknown,
        };

        mangaInfo.Chapters =
            document
                .DocumentNode.SelectNodes(".//div[@class='chapters']//div[@class='chapter']/a")
                ?.Reverse()
                ?.Select(el =>
                    (IMangaChapter)
                        new MangaChapter()
                        {
                            Id = el.Attributes["href"].Value,
                            Title = el.InnerText
                        }
                )
                .ToList() ?? [];

        return mangaInfo;
    }

    /// <inheritdoc />
    public async ValueTask<List<IMangaChapterPage>> GetChapterPagesAsync(
        string chapterId,
        CancellationToken cancellationToken = default!
    )
    {
        // server2 = lin + "?sv=mk";
        // server3 = lin + "?sv=3";

        var url = chapterId;
        var response = await _http.ExecuteAsync(url, cancellationToken);

        //var document = Html.Parse(response);
        //
        //var list = new List<IMangaChapterPage>();
        //
        //var i = 1;
        //list.AddRange(
        //    document.GetElementbyId("imgs").SelectNodes(".//img")
        //        .Select(el => new MangaChapterPage()
        //        {
        //            Image = el.Attributes["data-src"]!.Value,
        //            Page = i++
        //        })
        //);

        var list = new List<IMangaChapterPage>();

        //var urlMatches = Regex.Matches(response, @"(https?):\/\/(www\.)?[a-z0-9\.:].*?(?=\s)");
        var urlMatches = Regex.Matches(
            response,
            @"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&\/\/=]*)"
        );
        var uris = urlMatches
            .OfType<Match>()
            .Where(x => Uri.IsWellFormedUriString(x.Value, UriKind.Absolute))
            .Select(x => new Uri(x.Value))
            .Where(x => x.Host.ToLower().Contains("i1.mangakatana"))
            .ToList();

        var i = 1;
        list.AddRange(
            uris.Select(x => new MangaChapterPage() { Image = x.OriginalString, Page = i++ })
        );

        return list;
    }
}
