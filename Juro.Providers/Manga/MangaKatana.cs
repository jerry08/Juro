using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    private static readonly ConcurrentDictionary<string, List<IMangaResult>> SearchCache = new();
    private static readonly SemaphoreSlim SearchLock = new(1, 1);
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
        if (SearchCache.TryGetValue(query, out var cachedResults))
        {
            return [.. cachedResults];
        }

        await SearchLock.WaitAsync(cancellationToken);

        try
        {
            if (SearchCache.TryGetValue(query, out cachedResults))
            {
                return [.. cachedResults];
            }

            var url = $"{BaseUrl}/?search={Uri.EscapeDataString(query)}";
            List<IMangaResult> list = [];
            string response = string.Empty;

            for (var attempt = 0; attempt < 5; attempt++)
            {
                response = await _http.ExecuteAsync(url, GetBrowserHeaders(), cancellationToken);
                list = ParseSearchResults(response);
                if (list.Count > 0)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(250 * (attempt + 1)), cancellationToken);
            }

            var document = Html.Parse(response);

            var singleBookEl = document.GetElementbyId("single_book");
            if (singleBookEl is not null)
            {
                var result = new MangaResult()
                {
                    Title = singleBookEl
                        .SelectSingleNode(".//div[@class='info']/h1[@class='heading']")
                        ?.InnerText,
                    Image = singleBookEl.SelectSingleNode(".//img")?.Attributes["src"]?.Value,
                };

                var i = 0;

                var chapters = document
                    .DocumentNode.SelectNodes(
                        ".//div[@class='chapters']//div[@class='chapter']//a"
                    )!
                    .Select(el => new MangaChapterPage()
                    {
                        Image = el.Attributes["href"]!.Value,
                        Title = el.InnerText,
                        Page = i++,
                    })
                    .Reverse()
                    .ToList();

                var imgSplit = chapters.FirstOrDefault()!.Image.Split('/');

                result.Id = string.Join("/", imgSplit.Take(imgSplit.Length - 1));

                list.Add(result);
            }

            if (list.Count > 0)
            {
                SearchCache[query] = [.. list];
            }

            return list;
        }
        finally
        {
            SearchLock.Release();
        }
    }

    private List<IMangaResult> ParseSearchResults(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return [];

        var document = Html.Parse(response);
        var nodes = document.DocumentNode.SelectNodes(
            "//div[@id='book_list']//div[contains(@class, 'item')]"
        );
        if (nodes is null)
            return [];

        var list = new List<IMangaResult>();
        foreach (var node in nodes)
        {
            var linkNode =
                node.SelectSingleNode(
                    ".//h3[contains(@class, 'title')]//a[contains(@href, '/manga/')]"
                )
                ?? node.SelectSingleNode(
                    ".//div[contains(@class, 'wrap_img')]//a[contains(@href, '/manga/')]"
                );
            var href = linkNode?.GetAttributeValue("href", string.Empty);
            if (string.IsNullOrWhiteSpace(href))
                continue;

            // Covers are wrapped in <picture><source/><img/></picture>.
            var imageNode = node.SelectSingleNode(".//div[contains(@class, 'wrap_img')]//img");

            list.Add(
                new MangaResult()
                {
                    Id = href,
                    Title = WebUtility.HtmlDecode(linkNode!.InnerText).Trim(),
                    Image = imageNode?.GetAttributeValue("src", string.Empty),
                }
            );
        }

        return list.GroupBy(result => result.Id).Select(group => group.First()).ToList();
    }

    private Dictionary<string, string> GetBrowserHeaders() =>
        new()
        {
            {
                "Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8"
            },
            { "Accept-Language", "en-US,en;q=0.9" },
            { "Referer", $"{BaseUrl}/" },
        };

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

        mangaInfo.Title = document
            .DocumentNode.SelectSingleNode(".//div[@class='info']/h1[@class='heading']")
            ?.InnerText;
        mangaInfo.Description = document
            .DocumentNode.SelectSingleNode(".//div[@class='summary']/p")
            ?.InnerText.Trim();
        mangaInfo.Genres =
            document
                .DocumentNode.SelectNodes(".//div[@class='genres']/a")
                ?.Select(el => el.InnerText)
                .ToList()
            ?? [];

        var statusText = document
            .DocumentNode.SelectSingleNode(".//ul[@class='meta d-table']/li[4]/div[2]")
            ?.InnerText.Trim();
        mangaInfo.Status = statusText switch
        {
            "finished" or "completed" => MediaStatus.Completed,
            "publishing" => MediaStatus.Ongoing,
            _ => MediaStatus.Unknown,
        };

        var count = 1;
        var chapterNumberRegex = new Regex("([0-9]+(?:\\.[0-9]+)?)");

        mangaInfo.Chapters =
            document
                .DocumentNode.SelectNodes(".//div[@class='chapters']//div[@class='chapter']/a")
                ?.Reverse()
                ?.Select(el =>
                {
                    count++;

                    var title = el.InnerText;
                    var chapNum = chapterNumberRegex.Match(title)?.Groups[0].Value;

                    return (IMangaChapter)
                        new MangaChapter()
                        {
                            Id = el.Attributes["href"].Value,
                            Number = int.TryParse(chapNum, out var num) ? num : count,
                            Title = title,
                        };
                })
                .ToList()
            ?? [];

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
