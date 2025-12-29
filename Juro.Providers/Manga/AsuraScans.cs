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
/// Client for interacting with AsuraScans.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="AsuraScans"/>.
/// </remarks>
public class AsuraScans(IHttpClientFactory httpClientFactory) : IMangaProvider
{
    private readonly HttpClient _http = httpClientFactory.CreateClient();

    public string Key => Name;

    public string Name { get; set; } = "AsuraScans";

    public string Language => "en";

    public string BaseUrl => "https://asuracomic.net";

    public string Logo => "https://asuracomic.net/_next/image?url=%2Fimages%2Flogo.png&w=2048&q=75";

    /// <summary>
    /// Initializes an instance of <see cref="AsuraScans"/>.
    /// </summary>
    public AsuraScans(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="AsuraScans"/>.
    /// </summary>
    public AsuraScans()
        : this(Http.ClientProvider) { }

    /// <inheritdoc />
    public async ValueTask<List<IMangaResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default!
    )
    {
        var page = 1;

        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/series?page={page}&name={Uri.EscapeDataString(query)}",
            cancellationToken
        );

        var document = Html.Parse(response);

        var nodes = document
            //.DocumentNode.SelectNodes(".//div[@class='grid']/a")
            .DocumentNode.SelectNodes(".//div[contains(@class, 'grid')]/a")
            ?.Where(x => x.Attributes.Contains("href"))
            ?.ToList();
        if (nodes is null)
            return [];

        var list = new List<IMangaResult>();

        foreach (var node in nodes)
        {
            var rawUrl = node.Attributes["href"]!.Value;

            var id = GetMangaId(rawUrl);
            var url = GetMangaUrl(id);

            list.Add(
                new MangaResult()
                {
                    Id = id,
                    Title = node.SelectSingleNode(
                        ".//div[contains(@class, 'block')]/span[contains(@class, 'block')]"
                    )?.InnerText,
                    Image = Uri.UnescapeDataString(
                        BaseUrl + node.SelectSingleNode(".//img")!.Attributes["src"]!.Value
                    ),
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

        var url = GetMangaUrl(mangaId);
        var response = await _http.ExecuteAsync(url, cancellationToken);

        var document = Html.Parse(response);

        var wrapper = document.DocumentNode.SelectSingleNode(
            ".//div[contains(@class, 'relative') and contains(@class, 'grid')]"
        );

        mangaInfo.Title = document
            .DocumentNode.SelectSingleNode(
                ".//span[contains(@class, 'text-xl') and contains(@class, 'font-bold')]"
            )
            ?.InnerText;

        mangaInfo.Description = document
            .DocumentNode.SelectSingleNode(
                ".//span[contains(@class, 'font-medium') and contains(@class, 'text-sm')]"
            )
            ?.InnerText;

        mangaInfo.Headers = new() { { "Referer", BaseUrl } };

        mangaInfo.Image = Uri.UnescapeDataString(
            BaseUrl + wrapper?.SelectSingleNode(".//img[@alt='poster']")?.Attributes["src"].Value
        );

        mangaInfo.Genres =
            document
                .DocumentNode.SelectNodes(
                    ".//div[starts-with(@class, 'space')]//div[contains(@class, 'flex')]//button[contains(@class, 'text-white')]"
                )
                ?.Select(x => x.InnerText)
                .ToList()
            ?? [];

        var statusText = document
            .DocumentNode.SelectSingleNode(
                ".//div[contains(@class, 'flex')][h3[1][contains(text(), 'Status')]]/h3[2]"
            )
            ?.InnerText;

        mangaInfo.Status = statusText switch
        {
            "Ongoing" => MediaStatus.Ongoing,
            "Hiatus" => MediaStatus.Hiatus,
            "Completed" => MediaStatus.Completed,
            "Dropped" => MediaStatus.Cancelled,
            "Season End" => MediaStatus.Hiatus,
            _ => MediaStatus.Unknown,
        };

        var authorNode = document.DocumentNode.SelectSingleNode(
            ".//div[h3[1][text() = 'Author']]/h3[2]"
        );
        if (authorNode != null)
        {
            var text = authorNode.InnerText.Trim();
            if (text != "_")
            {
                mangaInfo.Authors = [text];
            }
        }

        var chapterNodes = document
            .DocumentNode.SelectNodes(
                ".//div[contains(@class, 'scrollbar-thumb-themecolor')]/div[contains(@class, 'group')]"
            )
            ?.Reverse();
        if (chapterNodes is null)
            return mangaInfo;

        foreach (var chapterNode in chapterNodes)
        {
            var rawUrl = BaseUrl + chapterNode.SelectSingleNode(".//a")?.Attributes["href"].Value;

            var id = GetChapterId(rawUrl);
            //var mangaId = GetMangaId(rawUrl);
            var url2 = GetChapterUrl(id, mangaId);

            // Extract the title
            var titleNode = chapterNode.SelectSingleNode(".//a/span");
            var title = titleNode?.InnerText.Trim() ?? string.Empty;

            // Extract the chapter text
            var chapterText =
                chapterNode.SelectSingleNode(".//a")?.InnerText.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(title))
                title = chapterText;

            // Remove title and "Chapter" from the chapter text
            var chapterNumberText = chapterText.Replace("Chapter", "").Trim();
            if (!string.IsNullOrEmpty(title))
                chapterNumberText = chapterNumberText.Replace(title, "");

            _ = int.TryParse(chapterNumberText, out var chapterNumber);

            mangaInfo.Chapters.Add(
                new MangaChapter()
                {
                    Id = url2,
                    Number = chapterNumber,
                    Title = title,
                }
            );
        }

        return mangaInfo;
    }

    /// <inheritdoc />
    public async ValueTask<List<IMangaChapterPage>> GetChapterPagesAsync(
        string chapterId,
        CancellationToken cancellationToken = default!
    )
    {
        var url = chapterId;
        var response = await _http.ExecuteAsync(url, cancellationToken);

        var document = Html.Parse(response);

        var nodes = document
            .DocumentNode.SelectNodes(".//img[contains(@alt, 'chapter')]")
            ?.ToList();

        var list = new List<IMangaChapterPage>();

        for (var i = 0; i < nodes?.Count; i++)
        {
            url = nodes[i].Attributes["src"]!.Value;

            // Extract the substring after the last '/'
            var afterLastSlash = url.Substring(url.LastIndexOf('/') + 1);

            // Extract the substring before the first '.'
            var beforeDot = afterLastSlash.Split('.')[0];

            if (!int.TryParse(beforeDot, out var pageIndex))
            {
                pageIndex = i + 1;
            }

            list.Add(
                new MangaChapterPage()
                {
                    Image = url,
                    Page = pageIndex,
                    Title = $"Page {pageIndex}",
                }
            );
        }

        return list;
    }

    /// <summary>
    /// Asura Scans appends a random string at the end of each series slug.
    /// The random string is not necessary, but we must leave the trailing '-' else
    /// the url will break.
    /// Example Url: https://asuracomic.net/series/swordmasters-youngest-son-cb22671f
    /// Example Url: https://asuracomic.net/series/swordmasters-youngest-son-cb22671f?blahblah
    /// Example Url: https://asuracomic.net/series/swordmasters-youngest-son-cb22671f/chapter/1
    /// parse "swordmasters-youngest-son-" from the url.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private static string GetMangaId(string url)
    {
        // Split the URL to ignore query parameters
        var path = url.Split('?').FirstOrDefault() ?? string.Empty;

        // Find the segment containing "series" and get the next segment
        var segments = path.Split('/');
        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i] == "series" && i + 1 < segments.Length)
            {
                var mangaSegment = segments[i + 1];
                var pos = mangaSegment.LastIndexOf('-');
                if (pos >= 0)
                {
                    var id = mangaSegment.Substring(0, pos + 1);
                    return id;
                }
            }
        }

        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns full URL of a manga from a manga ID.
    /// </summary>
    /// <param name="mangaId"></param>
    /// <returns></returns>
    public string GetMangaUrl(string mangaId) => $"{BaseUrl}/series/{mangaId}";

    /// <summary>
    /// Returns full URL of a chapter from a chapter ID and manga ID.
    /// </summary>
    /// <param name="chapterId"></param>
    /// <param name="mangaId"></param>
    /// <returns></returns>
    public string GetChapterUrl(string chapterId, string mangaId) =>
        $"{BaseUrl}/series/{mangaId}/chapter/{chapterId}";

    /// <summary>
    /// Returns the chapter ID of a chapter from a URL.
    /// </summary>
    /// <param name="url">The URL to extract the chapter ID from.</param>
    /// <returns>The chapter ID as a string.</returns>
    /// <exception cref="ArgumentException">Thrown if the URL does not contain a chapter ID.</exception>
    public static string GetChapterId(string url)
    {
        // Parse the URL
        var uri = new Uri(url);
        var path = uri.AbsolutePath;

        // Split the path into segments
        var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

        // Look for "chapter" in the path segments
        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i] == "chapter" && i + 1 < segments.Length)
            {
                // Extract the next segment which should be the chapter ID
                var chapterSegment = segments[i + 1];

                // Remove any trailing non-numeric characters
                var chapterId = RemoveTrailingNonNumericCharacters(chapterSegment);

                if (!string.IsNullOrEmpty(chapterId))
                {
                    return chapterId;
                }
                else
                {
                    throw new ArgumentException("Chapter ID is missing or invalid.");
                }
            }
        }

        throw new ArgumentException("Chapter ID not found in the URL.");
    }

    private static string RemoveTrailingNonNumericCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var endPos = input.Length;

        // Find the position where non-numeric characters start
        for (var i = 0; i < input.Length; i++)
        {
            if (!char.IsDigit(input[i]))
            {
                endPos = i;
                break;
            }
        }

        // Return the substring up to the position of the first non-numeric character
        return input.Substring(0, endPos);
    }
}
