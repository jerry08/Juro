using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Juro.Core;
using Juro.Core.Models.Anime;
using Juro.Core.Models.Videos;
using Juro.Core.Providers;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with AnimeGG.
/// </summary>
public class AnimeGG(IHttpClientFactory httpClientFactory)
    : AnimeBaseProvider(httpClientFactory),
        IAnimeProvider,
        IPopularProvider,
        ILastUpdatedProvider
{
    private static readonly Regex _episodeNumberRegex = new(
        @"(\d+(?:\.\d+)?)(?:-\d+(?:\.\d+)?)?$",
        RegexOptions.Compiled
    );
    private static readonly Regex _videoSourcesRegex = new(
        @"var\s+videoSources\s*=\s*(?<json>\[[\s\S]*?\]);",
        RegexOptions.Compiled
    );
    private static readonly Regex _objectRegex = new(@"\{[\s\S]*?\}", RegexOptions.Compiled);
    private static readonly Regex _fileRegex = new(
        @"['""']?file['""']?\s*:\s*['""'](?<value>[^'""']+)['""']",
        RegexOptions.Compiled
    );
    private static readonly Regex _labelRegex = new(
        @"['""']?label['""']?\s*:\s*['""'](?<value>[^'""']+)['""']",
        RegexOptions.Compiled
    );

    private readonly HttpClient _http = httpClientFactory.CreateClient();

    public string Key => Name;

    public string Name => "AnimeGG";

    public string Language => "en";

    public bool IsDubAvailableSeparately => true;

    public string BaseUrl => "https://www.animegg.org";

    /// <summary>
    /// Initializes an instance of <see cref="AnimeGG"/>.
    /// </summary>
    public AnimeGG(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="AnimeGG"/>.
    /// </summary>
    public AnimeGG()
        : this(Http.ClientProvider) { }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/search/?q={Uri.EscapeDataString(query)}",
            cancellationToken
        );

        var document = Html.Parse(response);
        var nodes = document.DocumentNode.SelectNodes("//a[contains(@class, 'mse')]");
        if (nodes is null)
            return [];

        return nodes.Select(ParseSearchCard).Where(x => x is not null).Cast<IAnimeInfo>().ToList();
    }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> GetPopularAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/popular-series?sortBy=hits&sortDirection=DESC&ongoing&limit=50&start=0",
            cancellationToken
        );

        return ParsePopularList(response);
    }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> GetLastUpdatedAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/popular-series?sortBy=createdAt&sortDirection=DESC&ongoing&limit=50&start=0",
            cancellationToken
        );

        return ParsePopularList(response);
    }

    /// <inheritdoc />
    public async ValueTask<IAnimeInfo> GetAnimeInfoAsync(
        string animeId,
        CancellationToken cancellationToken = default
    )
    {
        var url = BuildUrl(animeId);
        var response = await _http.ExecuteAsync(url, cancellationToken);
        var document = Html.Parse(response);

        var anime = new AnimeInfo
        {
            Id = ToRelativePath(url),
            Site = AnimeSites.AnimeGG,
            Link = url,
            Title =
                document
                    .DocumentNode.SelectSingleNode("//*[contains(@class, 'media-body')]//h1")
                    ?.InnerText.Trim()
                ?? string.Empty,
            Summary = document
                .DocumentNode.SelectSingleNode("//*[contains(@class, 'ptext')]")
                ?.InnerText.Trim(),
            Image = NormalizeUrl(
                document
                    .DocumentNode.SelectSingleNode(
                        "//*[contains(@class, 'media')]//*[contains(@class, 'media-object')]"
                    )
                    ?.GetAttributeValue("src", string.Empty)
            ),
            Status = url.Contains("/series/", StringComparison.OrdinalIgnoreCase)
                ? null
                : "Completed",
        };

        var status = document
            .DocumentNode.SelectNodes("//*[contains(@class, 'infoami')]//span")
            ?.Select(x => x.InnerText.Trim())
            .FirstOrDefault(x => x.Contains("Status", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(status))
            anime.Status = status!.Substring(status.IndexOf(':') + 1).Trim();

        anime.Genres =
            document
                .DocumentNode.SelectNodes("//*[contains(@class, 'tagscat')]//a")
                ?.Select(x => new Genre(x.InnerText.Trim()))
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .ToList()
            ?? [];

        return anime;
    }

    /// <inheritdoc />
    public async ValueTask<List<Episode>> GetEpisodesAsync(
        string animeId,
        CancellationToken cancellationToken = default
    )
    {
        var url = BuildUrl(animeId);
        var response = await _http.ExecuteAsync(url, cancellationToken);
        var document = Html.Parse(response);
        var nodes = document.DocumentNode.SelectNodes("//*[contains(@class, 'newmanga')]//li/div");
        if (nodes is null)
            return [];

        var episodes = new List<Episode>();
        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];
            var linkNode = node.SelectSingleNode(".//*[contains(@class, 'anm_det_pop')]");
            var href = linkNode?.GetAttributeValue("href", string.Empty);
            if (string.IsNullOrWhiteSpace(href))
                continue;

            var title =
                node.SelectSingleNode(".//*[contains(@class, 'anititle')]")?.InnerText.Trim()
                ?? string.Empty;
            var episodeNumber = ParseEpisodeNumber(linkNode?.InnerText) ?? index + 1;
            var formattedNumber = FormatEpisodeNumber(episodeNumber);

            episodes.Add(
                new Episode
                {
                    Id = ToRelativePath(BuildUrl(href!)),
                    Link = BuildUrl(href!),
                    Number = episodeNumber,
                    Name = title.Contains(formattedNumber, StringComparison.OrdinalIgnoreCase)
                        ? title
                        : $"Episode {formattedNumber} - {title}".TrimEnd(' ', '-'),
                    Description = node.SelectNodes(".//*[contains(@class, 'btn-xs')]")
                        ?.Select(x => x.InnerText.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .DefaultIfEmpty()
                        .Aggregate((left, right) => $"{left}, {right}"),
                }
            );
        }

        return episodes;
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    )
    {
        var url = BuildUrl(episodeId);
        var response = await _http.ExecuteAsync(url, cancellationToken);
        var document = Html.Parse(response);
        var nodes = document.DocumentNode.SelectNodes("//iframe[@src]");
        if (nodes is null)
            return [];

        return nodes
            .Select(node =>
            {
                var embedUrl = NormalizeUrl(node.GetAttributeValue("src", string.Empty));
                if (string.IsNullOrWhiteSpace(embedUrl))
                    return null;

                var mode = FindAncestor(node, "div")?.GetAttributeValue("id", string.Empty) switch
                {
                    "subbed-Animegg" => "[SUBBED]",
                    "dubbed-Animegg" => "[DUBBED]",
                    "raw-Animegg" => "[RAW]",
                    _ => string.Empty,
                };

                return new VideoServer
                {
                    Name = string.IsNullOrWhiteSpace(mode) ? "AnimeGG" : $"{mode} AnimeGG",
                    Embed = new(embedUrl!) { Headers = new() { ["Referer"] = url } },
                };
            })
            .Where(x => x is not null)
            .Cast<VideoServer>()
            .ToList();
    }

    /// <inheritdoc />
    public override async ValueTask<List<VideoSource>> GetVideosAsync(
        VideoServer server,
        CancellationToken cancellationToken = default
    )
    {
        var embedUrl = server.Embed.Url;
        if (!Uri.IsWellFormedUriString(embedUrl, UriKind.Absolute))
            return [];

        var response = await _http.ExecuteAsync(embedUrl, server.Embed.Headers, cancellationToken);

        var match = _videoSourcesRegex.Match(response);
        if (!match.Success)
            return [];

        var host = new Uri(embedUrl).GetLeftPart(UriPartial.Authority);
        var sources = new List<VideoSource>();
        foreach (Match objectMatch in _objectRegex.Matches(match.Groups["json"].Value))
        {
            var file = _fileRegex.Match(objectMatch.Value).Groups["value"].Value;
            if (string.IsNullOrWhiteSpace(file))
                continue;

            var label = _labelRegex.Match(objectMatch.Value).Groups["value"].Value;
            var videoUrl = NormalizeUrl(file.Replace("\\/", "/"), host);
            var isHls = videoUrl?.Contains(".m3u8", StringComparison.OrdinalIgnoreCase) == true;

            sources.Add(
                new VideoSource
                {
                    Title = label,
                    Resolution = label,
                    VideoUrl = videoUrl!,
                    Format = isHls ? VideoType.M3u8 : VideoType.Container,
                    FileType = isHls ? "m3u8" : "mp4",
                    Headers = new() { ["Referer"] = host },
                    VideoServer = server,
                }
            );
        }

        return sources;
    }

    private List<IAnimeInfo> ParsePopularList(string response)
    {
        var document = Html.Parse(response);
        var nodes = document.DocumentNode.SelectNodes("//*[contains(@class, 'fea')]");
        if (nodes is null)
            return [];

        return nodes.Select(ParsePopularCard).Where(x => x is not null).Cast<IAnimeInfo>().ToList();
    }

    private AnimeInfo? ParsePopularCard(HtmlNode element)
    {
        var linkNode = element.SelectSingleNode(".//*[contains(@class, 'rightpop')]//a");
        var title = linkNode?.InnerText.Trim();
        var href = linkNode?.GetAttributeValue("href", string.Empty);
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(href))
            return null;

        return new AnimeInfo
        {
            Id = ToRelativePath(BuildUrl(href!)),
            Site = AnimeSites.AnimeGG,
            Title = title!,
            Image = NormalizeUrl(element.SelectSingleNode(".//img")?.GetAttributeValue("src", "")),
            Link = BuildUrl(href!),
        };
    }

    private AnimeInfo? ParseSearchCard(HtmlNode element)
    {
        var title = element
            .SelectSingleNode(".//*[contains(@class, 'first')]//h2")
            ?.InnerText.Trim();
        var href = element.GetAttributeValue("href", string.Empty);
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(href))
            return null;

        return new AnimeInfo
        {
            Id = ToRelativePath(BuildUrl(href!)),
            Site = AnimeSites.AnimeGG,
            Title = title!,
            Image = NormalizeUrl(element.SelectSingleNode(".//img")?.GetAttributeValue("src", "")),
            Link = BuildUrl(href!),
        };
    }

    private string BuildUrl(string pathOrUrl) => NormalizeUrl(pathOrUrl) ?? BaseUrl;

    private string ToRelativePath(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        return uri.PathAndQuery;
    }

    private string? NormalizeUrl(string? url, string? baseUrl = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (url.StartsWith("//", StringComparison.Ordinal))
            return "https:" + url;

        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            return url;

        var origin = baseUrl ?? BaseUrl;
        if (url.StartsWith("/", StringComparison.Ordinal))
            return origin.TrimEnd('/') + url;

        return origin.TrimEnd('/') + "/" + url;
    }

    private static float? ParseEpisodeNumber(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var match = _episodeNumberRegex.Match(input.Trim());
        return match.Success && float.TryParse(match.Groups[1].Value, out var number)
            ? number
            : null;
    }

    private static string FormatEpisodeNumber(float number) =>
        number % 1 == 0 ? $"{number:0}" : $"{number:0.0}";

    private static HtmlNode? FindAncestor(HtmlNode node, string name)
    {
        var current = node.ParentNode;
        while (current is not null)
        {
            if (current.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return current;

            current = current.ParentNode;
        }

        return null;
    }
}
