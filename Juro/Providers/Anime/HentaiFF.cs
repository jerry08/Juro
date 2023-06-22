using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Extractors;
using Juro.Models;
using Juro.Models.Anime;
using Juro.Models.Videos;
using Juro.Utils;
using Juro.Utils.Extensions;

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with HentaiFF.
/// </summary>
public class HentaiFF : IAnimeProvider
{
    private readonly HttpClient _http;
    private readonly IHttpClientFactory _httpClientFactory;

    public virtual string Name => "HentaiFF";

    public string Language => "en";

    public bool IsDubAvailableSeparately => false;

    public virtual string BaseUrl => "https://hentaiff.com";

    /// <summary>
    /// Initializes an instance of <see cref="HentaiFF"/>.
    /// </summary>
    public HentaiFF(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient();
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Initializes an instance of <see cref="HentaiFF"/>.
    /// </summary>
    public HentaiFF(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider))
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="HentaiFF"/>.
    /// </summary>
    public HentaiFF() : this(Http.ClientProvider)
    {
    }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var list = new List<IAnimeInfo>();

        _http.Timeout = TimeSpan.FromSeconds(30);

        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/?s={query}",
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(response))
            return list;

        var document = Html.Parse(response);

        var nodes = document.DocumentNode
            //.SelectNodes(".//article[@class='bs']//div[@class='bsx']//a");
            .SelectNodes(".//article//div[@class='bsx']//a");

        foreach (var node in nodes)
        {
            var anime = new AnimeInfo
            {
                Id = node.Attributes["href"].Value
            };

            anime.Link = anime.Id;
            anime.Title = node.Attributes["title"].Value;
            anime.Image = node.SelectSingleNode(".//img").Attributes["src"].Value;
            anime.Status = node.SelectSingleNode(".//div[contains(@class, 'status')]")?
                .InnerText;
            anime.Type = node.SelectSingleNode(".//div[contains(@class, 'typez')]")?
                .InnerText;

            list.Add(anime);
        }

        return list;
    }

    /// <inheritdoc />
    public async ValueTask<IAnimeInfo> GetAnimeInfoAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async ValueTask<List<Episode>> GetEpisodesAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var list = new List<Episode>();

        var response = await _http.ExecuteAsync(id, cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
            return list;

        var document = Html.Parse(response);

        var nodes = document.DocumentNode
            .SelectNodes(".//div[@class='eplister']//ul//li//a").Reverse();

        foreach (var node in nodes)
        {
            var split = node.SelectSingleNode(".//div[@class='epl-num']")
                .InnerText.Split(' ');

            var episode = new Episode();

            episode.Id = node.Attributes["href"].Value;
            episode.Link = episode.Id;
            episode.Number = float.TryParse(split[0]?.ToString(), out var num) ? num : 0;
            episode.Name = split.ElementAtOrDefault(1)?.ToString();

            list.Add(episode);
        }

        return list;
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default)
    {
        var list = new List<VideoServer>();

        var response = await _http.ExecuteAsync(episodeId, cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
            return list;

        var document = Html.Parse(response);

        var nodes = document.DocumentNode
            .SelectNodes(".//select[@class='mirror']//option");

        foreach (var node in nodes)
        {
            var base64 = node.Attributes["value"].Value;
            var link = base64.DecodeBase64().SubstringAfter("src=\"").SubstringBefore("\"");

            if (string.IsNullOrWhiteSpace(link))
                continue;

            list.Add(new(node.InnerText, new FileUrl()
            {
                Url = link
            }));
        }

        return list;
    }

    public IVideoExtractor? GetVideoExtractor(VideoServer server)
    {
        if (server.Embed.Url.Contains("amhentai"))
            return new FPlayerExtractor(_httpClientFactory);
        else if (server.Embed.Url.Contains("cdnview"))
            return new CdnViewExtractor(_httpClientFactory);
        else if (server.Embed.Url.Contains("hentaistream"))
            return new HentaiStreamExtractor();

        return null;
    }

    private class CdnViewExtractor : IVideoExtractor
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public string ServerName => string.Empty;

        public CdnViewExtractor(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async ValueTask<List<VideoSource>> ExtractAsync(
            string url,
            CancellationToken cancellationToken = default)
        {
            var http = _httpClientFactory.CreateClient();

            var response = await http.ExecuteAsync(url, cancellationToken);

            var document = Html.Parse(response);

            var source = document.DocumentNode
                .SelectSingleNode(".//div[@class='source']").Attributes["src"].Value;

            var host = new Uri(url).Host;
            var link = $"https://{host}{source}";

            return new()
            {
                new()
                {
                    Format = VideoType.M3u8,
                    VideoUrl = link
                }
            };
        }
    }

    private class HentaiStreamExtractor : IVideoExtractor
    {
        public string ServerName => string.Empty;

        public ValueTask<List<VideoSource>> ExtractAsync(
            string url,
            CancellationToken cancellationToken = default)
        {
            var base64 = url.SubstringAfter("html#");
            var link = base64.DecodeBase64().SubstringAfter("url=").SubstringBefore(";");

            var headers = new Dictionary<string, string>()
            {
                ["Referer"] = "https://hentaistream.moe/"
            };

            var list = new List<VideoSource>()
            {
                new()
                {
                    Format = VideoType.Container,
                    VideoUrl = $"{link}x264.720p.mp4",
                    Resolution = "720p",
                    FileType = "MP4",
                    Headers = headers
                },
                new()
                {
                    Format = VideoType.Container,
                    VideoUrl = $"{link}av1.1080p.webm",
                    Resolution = "1080p",
                    FileType = "WEBM",
                    Headers = headers
                },
                new()
                {
                    Format = VideoType.Container,
                    VideoUrl = $"{link}av1.2160p.webm",
                    Resolution = "2160p",
                    FileType = "WEBM",
                    Headers = headers
                }
            };

            return new ValueTask<List<VideoSource>>(list);
        }
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoSource>> GetVideosAsync(
        VideoServer server,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.IsWellFormedUriString(server.Embed.Url, UriKind.Absolute))
            return new();

        var extractor = GetVideoExtractor(server);
        if (extractor is null)
            return new();

        return await extractor.ExtractAsync(server.Embed.Url, cancellationToken);
    }
}