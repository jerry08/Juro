using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using HtmlAgilityPack;
using Juro.Models.Manga;
using Juro.Utils.Extensions;

namespace Juro.Providers.Manga;

public class MangaKakalot : MangaParser
{
    public override string Name { get; set; } = "MangaKakalot";

    public override string BaseUrl => "https://mangakakalot.com";

    public override string Logo => "https://scontent-lga3-1.xx.fbcdn.net/v/t31.18172-8/23592342_1993674674222540_3098972633173711780_o.png?stp=cp0_dst-png_p64x64&_nc_cat=105&ccb=1-7&_nc_sid=85a577&_nc_ohc=j_WvAOX4tOwAX9dNL_4&_nc_ht=scontent-lga3-1.xx&oh=00_AT-ZFkuaHiS33j-oUCtn-jzwkLfVuCONx0aqF3QXrcFKvg&oe=62FC016C";

    public MangaKakalot(HttpClient httpClient) : base(httpClient)
    {
    }

    public override async Task<List<MangaResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default!)
    {
        query = query.Replace(" ", "_");

        var response = await _http.ExecuteAsync($"{BaseUrl}/search/story/{query}", cancellationToken);

        var document = new HtmlDocument();
        document.LoadHtml(response);

        var nodes = document.DocumentNode.SelectNodes(".//div[@class='daily-update']/div/div")?.ToList();
        if (nodes is null)
            return new();

        var mangas = new List<MangaResult>();
        foreach (var node in nodes)
        {
            mangas.Add(new()
            {
                Id = node.SelectSingleNode(".//div/h3/a")?.Attributes["href"]!.Value.Split('/')[3]!,
                Title = node.SelectSingleNode(".//div/h3/a")?.InnerText,
                Image = node.SelectSingleNode(".//a/img")?.Attributes["src"]?.Value,
                HeaderForImage = new() { { "Referer", BaseUrl } }
            });
        }

        return mangas;
    }

    public override async Task<MangaInfo> GetMangaInfoAsync(
        string mangaId,
        CancellationToken cancellationToken = default!)
    {
        var mangaInfo = new MangaInfo
        {
            Id = mangaId,
            Title = ""
        };

        var url = mangaId.Contains("read") ? BaseUrl : "https://readmanganato.com";

        var response = await _http.ExecuteAsync($"{url}/{mangaId}", cancellationToken);

        var document = new HtmlDocument();
        document.LoadHtml(response);

        if (url.Contains("mangakakalot"))
        {
            mangaInfo.Title = document.DocumentNode.SelectSingleNode(".//div[@class='manga-info-top']/ul/li[1]/h1")?.InnerText;
            mangaInfo.AltTitles = document.DocumentNode.SelectSingleNode(".//div[@class='manga-info-top']/ul/li[1]/h2")
                ?.InnerText.Replace("Alternative :", "").Split(';')?.Select(x => x.Trim()).ToList() ?? new();
            mangaInfo.Description = document.GetElementbyId("noidungm")
                ?.InnerText?.Replace($"{mangaInfo.Title} summary:", "").Replace(Environment.NewLine, "").Trim();
            mangaInfo.HeaderForImage = new() { { "Referer", BaseUrl } };
            mangaInfo.Image = document.DocumentNode.SelectSingleNode(".//div[@class='manga-info-top']/div/img")?.Attributes["src"]?.Value;
            mangaInfo.Genres = document.DocumentNode.SelectNodes(".//div[@class='manga-info-top']/ul/li[7]/a")
                ?.Select(x => x.InnerText).ToList() ?? new();

            var statusText = document.DocumentNode.SelectSingleNode(".//div[@class='manga-info-top']/ul/li[3]")?.InnerText?.Replace("Status :", "").Trim();
            mangaInfo.Status = statusText switch
            {
                "Completed" => MediaStatus.Completed,
                "Ongoing" => MediaStatus.Ongoing,
                _ => MediaStatus.Unknown,
            };

            mangaInfo.Views = document.DocumentNode.SelectSingleNode(".//div[@class='manga-info-top']/ul/li[6]")
                ?.InnerText?.Replace("View : ", "").Replace(Environment.NewLine, "").Trim();
            mangaInfo.Authors = document.DocumentNode.SelectNodes(".//div[@class='manga-info-top']/ul/li[2]/a")
                ?.Select(x => x.InnerText).ToList() ?? new();
            mangaInfo.Chapters = document.DocumentNode.SelectNodes(".//div[@class='chapter-list']/div[@class='row']")
                ?.Select(el => new MangaChapter()
                {
                    Id = el.SelectSingleNode(".//span/a").Attributes["href"].Value.Split(new[] { "chapter/" }, StringSplitOptions.None)[1],
                    Title = el.SelectSingleNode(".//span/a").InnerText,
                    Views = el.SelectSingleNode(".//span[2]")?.InnerText?.Replace(Environment.NewLine, "").Trim(),
                    ReleasedDate = el.SelectSingleNode(".//span[3]")?.Attributes["title"]?.Value
                }).ToList() ?? new();
        }
        else
        {
            mangaInfo.Title = document.DocumentNode.SelectSingleNode(".//div[@class='panel-story-info']/div[@class='story-info-right']/h1")?.InnerText;
            mangaInfo.AltTitles = document.DocumentNode.SelectSingleNode(".//div[@class='story-info-right']/table/tbody/tr[1]/td[@class='table-value']/h2")
                ?.InnerText.Replace("Alternative :", "").Split(';')?.Select(x => x.Trim()).ToList() ?? new();
            mangaInfo.Description = document.DocumentNode.SelectSingleNode(".//div[@id='panel-story-info-description']")
                ?.InnerText?.Replace("Description :", "").Replace(Environment.NewLine, "").Trim();
            mangaInfo.HeaderForImage = new() { { "Referer", BaseUrl } };
            mangaInfo.Image = document.DocumentNode.SelectSingleNode(".//div[@class='story-info-left']/span[@class='info-image']/img")?.Attributes["src"]?.Value;
            mangaInfo.Genres = document.DocumentNode.SelectNodes(".//div[@class='story-info-right']/table/tbody/tr[4]/td[@class='table-value']/a")
                ?.Select(x => x.InnerText).ToList() ?? new();

            var statusText = document.DocumentNode.SelectSingleNode(".//div[@class='story-info-right']/table/tbody/tr[3]/td[@class='table-value']")?.InnerText?.Trim();
            mangaInfo.Status = statusText switch
            {
                "Completed" => MediaStatus.Completed,
                "Ongoing" => MediaStatus.Ongoing,
                _ => MediaStatus.Unknown,
            };

            mangaInfo.Views = document.DocumentNode.SelectSingleNode(".//div[@class='story-info-right']/table/tbody/tr[3]/td[@class='table-value']")?.InnerText?.Trim();
            mangaInfo.Authors = document.DocumentNode.SelectNodes(".//div[@class='story-info-right']/table/tbody/tr[2]/td[@class='table-value']/a")
                ?.Select(x => x.InnerText).ToList() ?? new();
            mangaInfo.Chapters = document.DocumentNode.SelectNodes(".//div[@class='container-main-left']/div[@class='panel-story-chapter-list']/ul/li")
                ?.Select(el => new MangaChapter()
                {
                    Id = el.SelectSingleNode(".//a").Attributes["href"].Value.Split(new[] { ".com/" }, StringSplitOptions.None)[1] + "$$READMANGANATO",
                    Title = el.SelectSingleNode(".//a").InnerText,
                    Views = el.SelectSingleNode(".//span[@class='chapter-view text-nowrap']")?.InnerText?.Replace(Environment.NewLine, "").Trim(),
                    ReleasedDate = el.SelectSingleNode(".//span[@class='chapter-time text-nowrap']")?.Attributes["title"]?.Value
                }).ToList() ?? new();
        }

        return mangaInfo;
    }

    public override async Task<List<MangaChapterPage>> GetChapterPagesAsync(
        string chapterId,
        CancellationToken cancellationToken = default!)
    {
        var url = !chapterId.Contains("$$READMANGANATO")
            ? $"{BaseUrl}/chapter/{chapterId}"
            : $"https://readmanganato.com/{chapterId.Replace("$$READMANGANATO", "")}";

        var response = await _http.ExecuteAsync(url, cancellationToken);

        var document = new HtmlDocument();
        document.LoadHtml(response);

        int i = 1;

        var pages = document.DocumentNode.SelectNodes(".//div[@class='container-chapter-reader']/img")
            .Select(el => new MangaChapterPage()
            {
                Image = el.Attributes["src"]?.Value,
                Page = i++,
                Title = el.Attributes["alt"]?.Value
                    .Replace("- MangaNato.com", "")
                    .Replace("- Mangakakalot.com", "")
                    .Trim(),
                HeaderForImage = new() { { "Referer", BaseUrl } }
            }).ToList();

        return pages;
    }
}