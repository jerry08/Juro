using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core;
using Juro.Core.Models.Manga;
using Juro.Core.Providers;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;

namespace Juro.Providers.Manga;

/// <summary>
/// Client for interacting with MangaPill.
/// </summary>
public class MangaPill : IMangaProvider
{
    private readonly HttpClient _http;

    public string Key => Name;

    /// <inheritdoc />
    public string Name { get; set; } = "MangaPill";

    /// <inheritdoc />
    public string Language => "en";

    /// <inheritdoc />
    public string BaseUrl => "https://mangapill.com";

    /// <inheritdoc />
    public string Logo => "";

    /// <summary>
    /// Initializes an instance of <see cref="MangaPill"/>.
    /// </summary>
    public MangaPill(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient();
    }

    /// <summary>
    /// Initializes an instance of <see cref="MangaPill"/>.
    /// </summary>
    public MangaPill(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider))
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="MangaPill"/>.
    /// </summary>
    public MangaPill() : this(Http.ClientProvider)
    {
    }

    /// <inheritdoc/>
    public async ValueTask<List<IMangaResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default!)
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/quick-search?q={Uri.EscapeDataString(query)}",
            cancellationToken
        );

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

    /// <inheritdoc />
    public async ValueTask<IMangaInfo> GetMangaInfoAsync(
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

    /// <inheritdoc />
    public async ValueTask<List<IMangaChapterPage>> GetChapterPagesAsync(
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