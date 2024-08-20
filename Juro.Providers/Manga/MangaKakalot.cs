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
/// Client for interacting with MangaKakalot.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="MangaKakalot"/>.
/// </remarks>
public class MangaKakalot(IHttpClientFactory httpClientFactory) : IMangaProvider
{
    private readonly HttpClient _http = httpClientFactory.CreateClient();

    public string Key => Name;

    public string Name { get; set; } = "MangaKakalot";

    public string Language => "en";

    public string BaseUrl => "https://mangakakalot.com";

    public string Logo =>
        "https://scontent-lga3-1.xx.fbcdn.net/v/t31.18172-8/23592342_1993674674222540_3098972633173711780_o.png?stp=cp0_dst-png_p64x64&_nc_cat=105&ccb=1-7&_nc_sid=85a577&_nc_ohc=j_WvAOX4tOwAX9dNL_4&_nc_ht=scontent-lga3-1.xx&oh=00_AT-ZFkuaHiS33j-oUCtn-jzwkLfVuCONx0aqF3QXrcFKvg&oe=62FC016C";

    /// <summary>
    /// Initializes an instance of <see cref="MangaKakalot"/>.
    /// </summary>
    public MangaKakalot(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="MangaKakalot"/>.
    /// </summary>
    public MangaKakalot()
        : this(Http.ClientProvider) { }

    /// <inheritdoc />
    public async ValueTask<List<IMangaResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default!
    )
    {
        query = query.Replace(" ", "_");

        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/search/story/{query}",
            cancellationToken
        );

        var document = Html.Parse(response);

        var nodes = document
            .DocumentNode.SelectNodes(".//div[@class='daily-update']/div/div")
            ?.ToList();
        if (nodes is null)
            return [];

        var list = new List<IMangaResult>();

        foreach (var node in nodes)
        {
            list.Add(
                new MangaResult()
                {
                    Id = node.SelectSingleNode(".//div/h3/a")
                        ?.Attributes["href"]!.Value.Split('/')[3]!,
                    Title = node.SelectSingleNode(".//div/h3/a")?.InnerText,
                    Image = node.SelectSingleNode(".//a/img")?.Attributes["src"]?.Value,
                    Headers = new() { { "Referer", BaseUrl } },
                }
            );
        }

        return list;
    }

    /// <inheritdoc />
    public async ValueTask<IMangaInfo> GetMangaInfoAsync(
        string mangaId,
        CancellationToken cancellationToken = default!
    )
    {
        var mangaInfo = new MangaInfo { Id = mangaId, Title = string.Empty };

        var url = mangaId.Contains("read") ? BaseUrl : "https://chapmanganato.com";
        var response = await _http.ExecuteAsync($"{url}/{mangaId}", cancellationToken);

        var document = Html.Parse(response);

        var count = 1;
        var chapterNumberRegex = new Regex("((?<=Chapter )[0-9.]+)([\\s:]+)?(.+)?");

        if (url.Contains("mangakakalot"))
        {
            mangaInfo.Title = document
                .DocumentNode.SelectSingleNode(".//div[@class='manga-info-top']/ul/li[1]/h1")
                ?.InnerText;
            mangaInfo.AltTitles =
                document
                    .DocumentNode.SelectSingleNode(".//div[@class='manga-info-top']/ul/li[1]/h2")
                    ?.InnerText.Replace("Alternative :", "")
                    .Split(';')
                    ?.Select(x => x.Trim())
                    .ToList() ?? [];
            mangaInfo.Description = document
                .GetElementbyId("noidungm")
                ?.InnerText?.Replace($"{mangaInfo.Title} summary:", "")
                .Replace(Environment.NewLine, "")
                .Trim();
            mangaInfo.Headers = new() { { "Referer", BaseUrl } };
            mangaInfo.Image = document
                .DocumentNode.SelectSingleNode(".//div[@class='manga-info-top']/div/img")
                ?.Attributes["src"]
                ?.Value;
            mangaInfo.Genres =
                document
                    .DocumentNode.SelectNodes(".//div[@class='manga-info-top']/ul/li[7]/a")
                    ?.Select(x => x.InnerText)
                    .ToList() ?? [];

            var statusText = document
                .DocumentNode.SelectSingleNode(".//div[@class='manga-info-top']/ul/li[3]")
                ?.InnerText?.Replace("Status :", "")
                .Trim();
            mangaInfo.Status = statusText switch
            {
                "Completed" => MediaStatus.Completed,
                "Ongoing" => MediaStatus.Ongoing,
                _ => MediaStatus.Unknown,
            };

            mangaInfo.Views = document
                .DocumentNode.SelectSingleNode(".//div[@class='manga-info-top']/ul/li[6]")
                ?.InnerText?.Replace("View : ", "")
                .Replace(Environment.NewLine, "")
                .Trim();
            mangaInfo.Authors =
                document
                    .DocumentNode.SelectNodes(".//div[@class='manga-info-top']/ul/li[2]/a")
                    ?.Select(x => x.InnerText)
                    .ToList() ?? [];

            mangaInfo.Chapters =
                document
                    .DocumentNode.SelectNodes(".//div[@class='chapter-list']/div[@class='row']")
                    ?.Select(el =>
                    {
                        count++;

                        var title = el.SelectSingleNode(".//span/a").InnerText;
                        var chapNum = chapterNumberRegex.Match(title)?.Groups[0].Value;

                        return (IMangaChapter)
                            new MangaChapter()
                            {
                                Id = el.SelectSingleNode(".//span/a")
                                    .Attributes["href"]
                                    .Value.Split(new[] { "chapter/" }, StringSplitOptions.None)[1],
                                Number = int.TryParse(chapNum, out var num) ? num : count,
                                Title = title,
                                Views = el.SelectSingleNode(".//span[2]")
                                    ?.InnerText?.Replace(Environment.NewLine, "")
                                    .Trim(),
                                ReleasedDate = el.SelectSingleNode(".//span[3]")
                                    ?.Attributes["title"]
                                    ?.Value,
                            };
                    })
                    .ToList() ?? [];
        }
        else
        {
            mangaInfo.Title = document
                .DocumentNode.SelectSingleNode(
                    ".//div[@class='panel-story-info']/div[@class='story-info-right']/h1"
                )
                ?.InnerText;
            mangaInfo.AltTitles =
                document
                    .DocumentNode.SelectSingleNode(
                        ".//div[@class='story-info-right']/table/tbody/tr[1]/td[@class='table-value']/h2"
                    )
                    ?.InnerText.Replace("Alternative :", "")
                    .Split(';')
                    ?.Select(x => x.Trim())
                    .ToList() ?? [];
            mangaInfo.Description = document
                .DocumentNode.SelectSingleNode(".//div[@id='panel-story-info-description']")
                ?.InnerText?.Replace("Description :", "")
                .Replace(Environment.NewLine, "")
                .Trim();
            mangaInfo.Headers = new() { { "Referer", BaseUrl } };
            mangaInfo.Image = document
                .DocumentNode.SelectSingleNode(
                    ".//div[@class='story-info-left']/span[@class='info-image']/img"
                )
                ?.Attributes["src"]
                ?.Value;
            mangaInfo.Genres =
                document
                    .DocumentNode.SelectNodes(
                        ".//div[@class='story-info-right']/table/tbody/tr[4]/td[@class='table-value']/a"
                    )
                    ?.Select(x => x.InnerText)
                    .ToList() ?? [];

            var statusText = document
                .DocumentNode.SelectSingleNode(
                    ".//div[@class='story-info-right']/table/tbody/tr[3]/td[@class='table-value']"
                )
                ?.InnerText?.Trim();
            mangaInfo.Status = statusText switch
            {
                "Completed" => MediaStatus.Completed,
                "Ongoing" => MediaStatus.Ongoing,
                _ => MediaStatus.Unknown,
            };

            mangaInfo.Views = document
                .DocumentNode.SelectSingleNode(
                    ".//div[@class='story-info-right']/table/tbody/tr[3]/td[@class='table-value']"
                )
                ?.InnerText?.Trim();
            mangaInfo.Authors =
                document
                    .DocumentNode.SelectNodes(
                        ".//div[@class='story-info-right']/table/tbody/tr[2]/td[@class='table-value']/a"
                    )
                    ?.Select(x => x.InnerText)
                    .ToList() ?? [];

            mangaInfo.Chapters =
                document
                    .DocumentNode.SelectNodes(
                        ".//div[@class='container-main-left']/div[@class='panel-story-chapter-list']/ul/li"
                    )
                    .Reverse()
                    ?.Select(el =>
                    {
                        count++;

                        var title = el.SelectSingleNode(".//a").InnerText;
                        var chapNum = chapterNumberRegex.Match(title)?.Groups[0].Value;

                        return (IMangaChapter)
                            new MangaChapter()
                            {
                                //Id = el.SelectSingleNode(".//a").Attributes["href"].Value.Split(new[] { ".com/" }, StringSplitOptions.None)[1] + "$$READMANGANATO",
                                Id = el.SelectSingleNode(".//a").Attributes["href"].Value,
                                Title = title,
                                Number = int.TryParse(chapNum, out var num) ? num : count,
                                Views = el.SelectSingleNode(
                                        ".//span[@class='chapter-view text-nowrap']"
                                    )
                                    ?.InnerText?.Replace(Environment.NewLine, "")
                                    .Trim(),
                                ReleasedDate = el.SelectSingleNode(
                                        ".//span[@class='chapter-time text-nowrap']"
                                    )
                                    ?.Attributes["title"]
                                    ?.Value,
                            };
                    })
                    .ToList() ?? [];
        }

        return mangaInfo;
    }

    /// <inheritdoc />
    public async ValueTask<List<IMangaChapterPage>> GetChapterPagesAsync(
        string chapterId,
        CancellationToken cancellationToken = default!
    )
    {
        //var url = !chapterId.Contains("$$READMANGANATO")
        //    ? $"{BaseUrl}/chapter/{chapterId}"
        //    : $"https://readmanganato.com/{chapterId.Replace("$$READMANGANATO", "")}";

        var url = chapterId;
        var response = await _http.ExecuteAsync(url, cancellationToken);

        var document = Html.Parse(response);

        var i = 1;

        var pages = document
            .DocumentNode.SelectNodes(".//div[@class='container-chapter-reader']/img")
            .Select(el =>
                (IMangaChapterPage)
                    new MangaChapterPage()
                    {
                        Image = el.Attributes["src"]!.Value,
                        Page = i++,
                        Title = el.Attributes["alt"]
                            ?.Value.Replace("- MangaNato.com", "")
                            .Replace("- Mangakakalot.com", "")
                            .Trim(),
                        Headers = new() { { "Referer", BaseUrl } },
                    }
            )
            .ToList();

        return pages;
    }
}
