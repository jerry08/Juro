using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
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
/// Client for interacting with AsuraScans.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="AsuraScans"/>.
/// </remarks>
public class AsuraScans(IHttpClientFactory httpClientFactory) : IMangaProvider
{
    private const string ApiUrl = "https://api.asurascans.com";

    private readonly HttpClient _http = httpClientFactory.CreateClient();

    public string Key => Name;

    public string Name { get; set; } = "AsuraScans";

    public string Language => "en";

    public string BaseUrl => "https://asurascans.com";

    public string Logo => $"{BaseUrl}/images/logo.webp";

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
        var response = await _http.ExecuteAsync(
            $"{ApiUrl}/api/series?search={Uri.EscapeDataString(query)}&limit=30&offset=0",
            cancellationToken
        );

        var list = new List<IMangaResult>();
        var nodes = JsonNode.Parse(response)?["data"] as JsonArray;
        if (nodes is null)
            return list;

        foreach (var node in nodes)
        {
            var rawUrl = GetString(node, "public_url");
            var slug = GetString(node, "slug");
            if (string.IsNullOrWhiteSpace(rawUrl))
            {
                if (string.IsNullOrWhiteSpace(slug))
                    continue;

                rawUrl = $"/comics/{slug}";
            }

            var id = GetMangaId(rawUrl);

            list.Add(
                new MangaResult()
                {
                    Id = id,
                    Title = GetString(node, "title"),
                    Image = GetString(node, "cover") ?? GetString(node, "cover_url"),
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
        var apiSlug = GetApiSlug(mangaId);

        var response = await _http.ExecuteAsync(
            $"{ApiUrl}/api/series/{Uri.EscapeDataString(apiSlug)}",
            cancellationToken
        );
        var series = JsonNode.Parse(response)?["series"];
        if (series is null)
            return mangaInfo;

        mangaInfo.Title = GetString(series, "title");
        mangaInfo.Description = GetPlainText(GetString(series, "description"));
        mangaInfo.Headers = new() { { "Referer", BaseUrl } };
        mangaInfo.Image = GetString(series, "cover") ?? GetString(series, "cover_url");
        mangaInfo.AltTitles = GetStringArray(series["alt_titles"]);
        mangaInfo.Genres =
            (series["genres"] as JsonArray)
                ?.Select(genre => GetString(genre, "name"))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Cast<string>()
                .ToList()
            ?? [];

        mangaInfo.Status = GetString(series, "status")?.Trim().ToLowerInvariant() switch
        {
            "ongoing" => MediaStatus.Ongoing,
            "hiatus" => MediaStatus.Hiatus,
            "completed" => MediaStatus.Completed,
            "dropped" => MediaStatus.Cancelled,
            "season end" => MediaStatus.Hiatus,
            _ => MediaStatus.Unknown,
        };

        var author = GetString(series, "author")?.Trim();
        if (!string.IsNullOrWhiteSpace(author) && author != "_")
        {
            mangaInfo.Authors = [author];
        }

        var publicUrl = GetString(series, "public_url");
        var publicMangaId = !string.IsNullOrWhiteSpace(publicUrl) ? GetMangaId(publicUrl) : mangaId;
        var chaptersResponse = await _http.ExecuteAsync(
            $"{ApiUrl}/api/series/{Uri.EscapeDataString(apiSlug)}/chapters",
            cancellationToken
        );
        var chapters = JsonNode.Parse(chaptersResponse)?["data"] as JsonArray;
        if (chapters is not null)
        {
            foreach (var chapter in chapters)
            {
                if (!TryGetFloat(chapter?["number"], out var chapterNumber))
                    continue;

                var chapterId = chapterNumber.ToString(CultureInfo.InvariantCulture);
                var title = GetString(chapter, "title");

                mangaInfo.Chapters.Add(
                    new MangaChapter()
                    {
                        Id = GetChapterUrl(chapterId, publicMangaId),
                        Number = chapterNumber,
                        Title = string.IsNullOrWhiteSpace(title) ? $"Chapter {chapterId}" : title,
                        ReleasedDate = GetString(chapter, "published_at"),
                    }
                );
            }
        }

        mangaInfo.Chapters = mangaInfo.Chapters.OrderBy(chapter => chapter.Number).ToList();

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
            .DocumentNode.SelectNodes(
                ".//img[starts-with(@alt, 'Page ') or contains(translate(@alt, 'CHAPTER', 'chapter'), 'chapter')]"
            )
            ?.ToList();

        var list = new List<IMangaChapterPage>();

        for (var i = 0; i < nodes?.Count; i++)
        {
            url = nodes[i].GetAttributeValue("src", string.Empty);
            if (string.IsNullOrWhiteSpace(url))
                url = nodes[i].GetAttributeValue("data-src", string.Empty);
            if (string.IsNullOrWhiteSpace(url))
                continue;

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

        if (list.Count == 0)
        {
            AddSerializedReaderPages(document, list);
        }

        return list.GroupBy(page => page.Image)
            .Select(group => group.First())
            .OrderBy(page => page.Page)
            .ToList();
    }

    /// <summary>
    /// Asura Scans appends an eight-character cache suffix to public series URLs.
    /// Preserve it in public IDs because the website requires the complete route.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private static string GetMangaId(string url)
    {
        var path = url.Split('?').FirstOrDefault() ?? string.Empty;

        var segments = path.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (segments[i] is "comics" or "series")
            {
                return segments[i + 1];
            }
        }

        throw new ArgumentException($"Unable to parse manga id from '{url}'.", nameof(url));
    }

    /// <summary>
    /// Returns full URL of a manga from a manga ID.
    /// </summary>
    /// <param name="mangaId"></param>
    /// <returns></returns>
    public string GetMangaUrl(string mangaId) => $"{BaseUrl}/comics/{mangaId}";

    /// <summary>
    /// Returns full URL of a chapter from a chapter ID and manga ID.
    /// </summary>
    /// <param name="chapterId"></param>
    /// <param name="mangaId"></param>
    /// <returns></returns>
    public string GetChapterUrl(string chapterId, string mangaId) =>
        $"{BaseUrl}/comics/{mangaId}/chapter/{chapterId}";

    private static string GetApiSlug(string mangaId)
    {
        var id = mangaId.Contains("/") ? GetMangaId(mangaId) : mangaId;
        return Regex.Replace(id, "-[a-f0-9]{8}$", string.Empty, RegexOptions.IgnoreCase);
    }

    private static string? GetString(JsonNode? node, string propertyName) =>
        node?[propertyName]?.GetValue<string>();

    private static List<string> GetStringArray(JsonNode? node) =>
        (node as JsonArray)
            ?.Select(value => value?.GetValue<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToList()
        ?? [];

    private static string? GetPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return html;

        return WebUtility.HtmlDecode(Html.Parse(html).DocumentNode.InnerText).Trim();
    }

    private static bool TryGetFloat(JsonNode? node, out float value) =>
        float.TryParse(
            node?.ToString(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value
        );

    private static void AddSerializedReaderPages(
        HtmlAgilityPack.HtmlDocument document,
        List<IMangaChapterPage> pages
    )
    {
        var reader = document.DocumentNode.SelectSingleNode(
            ".//astro-island[contains(@component-url, 'ChapterReader')]"
        );
        var props = reader?.GetAttributeValue("props", string.Empty);
        if (string.IsNullOrWhiteSpace(props))
            return;

        JsonArray? serializedPages;
        try
        {
            serializedPages =
                JsonNode.Parse(WebUtility.HtmlDecode(props))?["pages"]?[1] as JsonArray;
        }
        catch (System.Text.Json.JsonException)
        {
            return;
        }

        if (serializedPages is null)
            return;

        for (var i = 0; i < serializedPages.Count; i++)
        {
            var image = serializedPages[i]?[1]?["url"]?[1]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(image))
                continue;

            pages.Add(
                new MangaChapterPage()
                {
                    Image = image,
                    Page = i + 1,
                    Title = $"Page {i + 1}",
                }
            );
        }
    }

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
