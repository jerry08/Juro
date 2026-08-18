using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Juro.Core;
using Juro.Core.Models;
using Juro.Core.Models.Anime;
using Juro.Core.Models.Videos;
using Juro.Core.Providers;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;
using Juro.Core.Utils.Tasks;
using Juro.Extractors;

namespace Juro.Providers.Anime;

/// <summary>
/// Shared implementation for sites running the "Anikoto" theme
/// (Aniwave/animewave.to, anikototv.to, and their mirrors). The sites share
/// markup, the VRF token scheme and the embed-player APIs.
/// </summary>
public abstract class AnikotoThemeProvider(IHttpClientFactory httpClientFactory)
    : AnimeBaseProvider(httpClientFactory),
        IAnimeProvider,
        IPopularProvider,
        ILastUpdatedProvider
{
    private static readonly Regex _episodeUrlSuffixRegex = new(
        @"/ep-\d+(?:\.\d+)?$",
        RegexOptions.Compiled
    );
    private static readonly Regex _softSubRegex = new(
        @"\bsoftsub\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );
    private static readonly Regex _dataIdRegex = new(
        "data-id=\"(?<id>[^\"]+)\"",
        RegexOptions.Compiled
    );
    private static readonly Regex _iframeSrcRegex = new(
        "<iframe[^>]+src=\"(?<src>[^\"]+)\"",
        RegexOptions.Compiled
    );
    private static readonly Regex _m3u8Regex = new(
        @"https?://[^\s""'<>]+\.m3u8[^\s""'<>]*",
        RegexOptions.Compiled
    );
    private static readonly Regex _sourceTagRegex = new(
        "<source[^>]+src=\"(?<src>[^\"]+\\.m3u8[^\"]*)\"",
        RegexOptions.Compiled
    );
    private static readonly Regex _jsVarM3u8Regex = new(
        """(?:var|let|const)\s+\w+\s*=\s*["'](?<url1>[^"']*(?:\.m3u8|/stream/)[^"']*)["']|(?:file|source|url|src)\s*[:=]\s*["'](?<url2>[^"']*(?:\.m3u8|/stream/)[^"']*)["']""",
        RegexOptions.Compiled
    );
    private static readonly Regex _hostMapRegex = new(
        @"var HOST_MAP\s*=\s*\{(?<map>[^}]+)\}",
        RegexOptions.Compiled
    );
    private static readonly Regex _hostEntryRegex = new(
        @"'(?<origin>[^']+)'\s*:\s*'(?<proxy>[^']+)'",
        RegexOptions.Compiled
    );
    private static readonly Regex _hlsUriAttributeRegex = new(
        "URI=\\\"(?<uri>[^\\\"]+)\\\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );
    private static readonly TimeSpan _sourceProbeTimeout = TimeSpan.FromSeconds(15);

    private readonly HttpClient _http = httpClientFactory.CreateClient();
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public abstract string Name { get; }

    public abstract string BaseUrl { get; }

    /// <summary>
    /// Site identifier stamped on parsed <see cref="AnimeInfo"/> results.
    /// </summary>
    protected abstract AnimeSites Site { get; }

    public string Key => Name;

    public string Language => "en";

    public bool IsDubAvailableSeparately => true;

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        var vrf = string.IsNullOrWhiteSpace(query) ? string.Empty : VrfEncrypt(query);
        var url = $"{BaseUrl}/filter?keyword={Uri.EscapeDataString(query)}&page=1&vrf={vrf}";
        var response = await _http.ExecuteAsync(url, BuildHeaders(BaseUrl), cancellationToken);
        return ParseAnimeResponse(response);
    }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> GetPopularAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/most-viewed/?page={page}",
            BuildHeaders(BaseUrl),
            cancellationToken
        );

        return ParseAnimeResponse(response);
    }

    /// <inheritdoc />
    public async ValueTask<List<IAnimeInfo>> GetLastUpdatedAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/latest-updated/?page={page}",
            BuildHeaders(BaseUrl),
            cancellationToken
        );

        return ParseAnimeResponse(response);
    }

    /// <inheritdoc />
    public async ValueTask<IAnimeInfo> GetAnimeInfoAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var animePath = NormalizeAnimePath(id);
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}{animePath}",
            BuildHeaders(BaseUrl),
            cancellationToken
        );
        var document = Html.Parse(response);
        var titleNode = document.DocumentNode.SelectSingleNode(
            "//h1[contains(@class, 'title')] | //h2[contains(@class, 'title')]"
        );
        var animeId =
            document
                .DocumentNode.SelectSingleNode("//*[@data-id]")
                ?.GetAttributeValue("data-id", string.Empty)
            ?? document
                .DocumentNode.SelectSingleNode("//*[@data-tip]")
                ?.GetAttributeValue("data-tip", string.Empty);
        var imageNode = document.DocumentNode.SelectSingleNode(
            "//div[contains(@class, 'poster')]//img"
        );

        var anime = new AnimeInfo
        {
            Id = string.IsNullOrWhiteSpace(animeId) ? animePath : $"{animePath}#{animeId}",
            Site = Site,
            Title = GetTitle(titleNode),
            OtherNames = titleNode?.GetAttributeValue("data-jp", string.Empty),
            Image = DefaultIfBlank(
                imageNode?.GetAttributeValue("data-src", string.Empty),
                imageNode?.GetAttributeValue("src", string.Empty)
            ),
            Summary = document
                .DocumentNode.SelectSingleNode(
                    "//div[contains(@class, 'synopsis')]//div[contains(@class, 'content')] | //div[@class='content']"
                )
                ?.InnerText.Trim(),
            Link = $"{BaseUrl}{animePath}",
        };

        anime.Genres =
            document
                .DocumentNode.SelectNodes("//div[contains(., 'Genres')]/span/a")
                ?.Select(x => new Genre(x.InnerText.Trim()))
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .ToList()
            ?? [];
        anime.Category = SelectMetaValue(document, "Studios");
        anime.Status = SelectMetaValue(document, "Status");
        anime.Released = SelectMetaValue(document, "Aired");
        anime.Type = SelectMetaValue(document, "Type");

        return anime;
    }

    /// <inheritdoc />
    public async ValueTask<List<Episode>> GetEpisodesAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var animePath = NormalizeAnimePath(id);
        var animeId = ExtractFragment(id);
        if (string.IsNullOrWhiteSpace(animeId))
            animeId = await ResolveAnimeIdAsync(animePath, cancellationToken);

        if (string.IsNullOrWhiteSpace(animeId))
            return [];

        var referer = $"{BaseUrl}{animePath}";
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/ajax/episode/list/{animeId}?vrf={VrfEncrypt(animeId!)}",
            BuildAjaxHeaders(referer),
            cancellationToken
        );
        var html = JsonNode.Parse(response)?["result"]?.ToString();
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var document = Html.Parse(html!);
        var nodes = document.DocumentNode.SelectNodes(
            "//div[contains(@class, 'episodes')]//ul/li/a"
        );
        if (nodes is null)
            return [];

        return nodes.Select(node => ParseEpisode(node, animePath)).Reverse().ToList();
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    )
    {
        var ids = Before(episodeId, "&");
        if (string.IsNullOrWhiteSpace(ids))
            ids = episodeId;

        var epUrl = Before(After(episodeId, "epurl="), "&");
        if (string.IsNullOrWhiteSpace(epUrl))
            epUrl = BaseUrl;

        var referer = epUrl.StartsWith("http") ? epUrl : $"{BaseUrl}{epUrl}";
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/ajax/server/list?servers={Uri.EscapeDataString(ids)}",
            BuildAjaxHeaders(referer),
            cancellationToken
        );
        var html = JsonNode.Parse(response)?["result"]?.ToString();
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var document = Html.Parse(html!);
        var nodes = document
            .DocumentNode.SelectNodes(
                "//div[contains(@class, 'servers')]/div[contains(@class, 'type')]/ul/li"
            )
            ?.ToList();
        if (nodes is null)
            return [];

        var functions = nodes.Select(node =>
            (Func<Task<VideoServer?>>)(
                async () => await GetVideoServerAsync(node, referer, cancellationToken)
            )
        );

        return (await TaskEx.Run(functions, 10))
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
        if (!Uri.IsWellFormedUriString(server.Embed.Url, UriKind.Absolute))
            return [];

        // External hosters (Filemoon, StreamTape, Mp4upload, …) keep their
        // dedicated extractors; everything else goes through the theme's
        // universal embed-player chain.
        var extractor = GetVideoExtractor(server);
        if (extractor is not null)
        {
            var videos = await base.GetVideosAsync(server, cancellationToken);
            if (videos.Count > 0)
                return videos;
        }

        try
        {
            return await ExtractFromEmbedAsync(
                server.Embed.Url,
                server,
                $"{BaseUrl}/",
                depth: 0,
                cancellationToken
            );
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public override IVideoExtractor? GetVideoExtractor(VideoServer server)
    {
        var lowerName = server.Name.ToLowerInvariant();
        if (lowerName.IndexOf("filemoon", StringComparison.Ordinal) >= 0)
            return new FilemoonExtractor(_httpClientFactory);
        if (lowerName.IndexOf("streamtape", StringComparison.Ordinal) >= 0)
            return new StreamTapeExtractor(_httpClientFactory);
        if (lowerName.IndexOf("mp4upload", StringComparison.Ordinal) >= 0)
            return new Mp4uploadExtractor(_httpClientFactory);

        return base.GetVideoExtractor(server);
    }

    #region Embed extraction

    private async ValueTask<List<VideoSource>> ExtractFromEmbedAsync(
        string embedUrl,
        VideoServer server,
        string referer,
        int depth,
        CancellationToken cancellationToken
    )
    {
        if (depth > 3 || !Uri.TryCreate(embedUrl, UriKind.Absolute, out var uri))
            return [];

        if (embedUrl.Contains("mewcdn.online/player/plyr.php"))
            return await ExtractFromMewcdnAsync(embedUrl, server, cancellationToken);

        if (
            embedUrl.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)
            || (ContainsIgnoreCase(embedUrl, ".m3u8") && !ContainsIgnoreCase(embedUrl, "/stream/"))
        )
        {
            return BuildHlsSources(embedUrl, server, referer);
        }

        var pageBody = await _http.ExecuteAsync(
            embedUrl,
            new Dictionary<string, string>
            {
                ["Referer"] = referer,
                ["User-Agent"] = Http.ChromeUserAgent(),
            },
            cancellationToken
        );
        if (string.IsNullOrWhiteSpace(pageBody))
            return [];

        var origin = uri.GetLeftPart(UriPartial.Authority);

        var dataId = _dataIdRegex.Match(pageBody).Groups["id"].Value;
        if (!string.IsNullOrWhiteSpace(dataId))
        {
            var videos = await FetchSourcesFromApiAsync(
                dataId,
                origin,
                embedUrl,
                server,
                cancellationToken
            );
            if (videos.Count > 0)
                return videos;
        }

        var iframe = _iframeSrcRegex.Match(pageBody).Groups["src"].Value;
        if (!string.IsNullOrWhiteSpace(iframe))
        {
            var videos = await ExtractFromEmbedAsync(
                NormalizeUrl(iframe, embedUrl),
                server,
                embedUrl,
                depth + 1,
                cancellationToken
            );
            if (videos.Count > 0)
                return videos;
        }

        var directM3u8 = _m3u8Regex.Match(pageBody).Value;
        if (!string.IsNullOrWhiteSpace(directM3u8))
            return BuildHlsSources(directM3u8, server, $"{origin}/");

        var sourceTag = _sourceTagRegex.Match(pageBody).Groups["src"].Value;
        if (!string.IsNullOrWhiteSpace(sourceTag))
            return BuildHlsSources(NormalizeUrl(sourceTag, embedUrl), server, $"{origin}/");

        var jsVarMatch = _jsVarM3u8Regex.Match(pageBody);
        var jsVarUrl = DefaultIfBlank(
            jsVarMatch.Groups["url1"].Value,
            jsVarMatch.Groups["url2"].Value
        );
        if (!string.IsNullOrWhiteSpace(jsVarUrl))
        {
            var resolved = NormalizeUrl(jsVarUrl, embedUrl);
            if (ContainsIgnoreCase(resolved, ".m3u8") || ContainsIgnoreCase(resolved, "/stream/"))
                return await FetchSourcesFromPageAsync(
                    resolved,
                    server,
                    $"{origin}/",
                    cancellationToken
                );
        }

        return [];
    }

    private async ValueTask<List<VideoSource>> FetchSourcesFromApiAsync(
        string dataId,
        string origin,
        string embedUrl,
        VideoServer server,
        CancellationToken cancellationToken
    )
    {
        var apiHeaders = new Dictionary<string, string>
        {
            ["Accept"] = "*/*",
            ["User-Agent"] = Http.ChromeUserAgent(),
            ["X-Requested-With"] = "XMLHttpRequest",
            ["Referer"] = embedUrl,
            ["Origin"] = origin,
        };

        var id = Uri.EscapeDataString(dataId);
        var streamType = GetStreamType(embedUrl);
        var sourceSelector = GetSourceSelector(embedUrl);
        var currentUrl = $"{origin}/stream/getSourcesNew?id={id}&id={id}";
        if (streamType is not null)
            currentUrl += $"&type={streamType}&type={streamType}";
        if (sourceSelector is not null)
            currentUrl += $"&s={Uri.EscapeDataString(sourceSelector)}";

        var legacyUrl = $"{origin}/stream/getSources?id={id}&id={id}";
        var candidateApiUrls = new List<string> { currentUrl };
        if (sourceSelector is not null)
            candidateApiUrls.Add($"{legacyUrl}&s={Uri.EscapeDataString(sourceSelector)}");
        candidateApiUrls.Add(legacyUrl);

        // A successful master manifest is not enough to prove an HLS source is
        // usable: stale manifests can reference dead segment hosts, and some
        // fallback CDNs return image decoys. Probe the first media segment and
        // fall through to the next API/CDN candidate when it is not media.
        var playbackHeaders = BuildPlaybackHeaders(origin);
        var checkedVideoUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidateApiUrl in candidateApiUrls.Distinct())
        {
            var data = await TryGetJsonAsync(candidateApiUrl, apiHeaders, cancellationToken);
            foreach (var videoUrl in ExtractVideoUrls(data?["sources"]))
            {
                if (!IsHttpUrl(videoUrl) || !checkedVideoUrls.Add(videoUrl))
                    continue;

                if (!await IsPlayableVideoSourceAsync(videoUrl, playbackHeaders, cancellationToken))
                    continue;

                var subtitles = ParsePlayerSubtitles(data?["tracks"] as JsonArray);
                var isDash = ContainsIgnoreCase(videoUrl, ".mpd");
                var isHls = ContainsIgnoreCase(videoUrl, ".m3u8");
                return
                [
                    new VideoSource
                    {
                        Title = server.Name,
                        Resolution = "Multi Quality",
                        VideoUrl = videoUrl,
                        Format =
                            isDash ? VideoType.Dash
                            : isHls ? VideoType.M3u8
                            : VideoType.Container,
                        FileType =
                            isDash ? "mpd"
                            : isHls ? "m3u8"
                            : GetFileType(videoUrl),
                        Headers = playbackHeaders,
                        Subtitles = subtitles,
                        VideoServer = server,
                    },
                ];
            }
        }

        return [];
    }

    private async ValueTask<bool> IsPlayableVideoSourceAsync(
        string videoUrl,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken
    )
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_sourceProbeTimeout);
        var probeToken = timeout.Token;

        try
        {
            if (ContainsIgnoreCase(videoUrl, ".m3u8"))
                return await IsPlayableHlsAsync(videoUrl, headers, probeToken);

            if (ContainsIgnoreCase(videoUrl, ".mpd"))
            {
                var manifest = await _http.ExecuteAsync(videoUrl, headers, probeToken);
                return ContainsIgnoreCase(manifest, "<MPD");
            }

            var prefix = await ReadMediaPrefixAsync(videoUrl, headers, probeToken);
            return prefix is not null && IsMediaPayload(prefix.Value.Buffer, prefix.Value.Length);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UriFormatException)
        {
            return false;
        }
    }

    private async ValueTask<bool> IsPlayableHlsAsync(
        string masterUrl,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken
    )
    {
        var playlistUrl = masterUrl;
        for (var depth = 0; depth < 3; depth++)
        {
            var playlist = await _http.ExecuteAsync(playlistUrl, headers, cancellationToken);
            if (!playlist.TrimStart().StartsWith("#EXTM3U", StringComparison.Ordinal))
                return false;

            var lines = playlist
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToList();
            var firstUri = lines.FirstOrDefault(line => !line.StartsWith("#"));
            if (string.IsNullOrWhiteSpace(firstUri))
                return false;

            if (lines.Any(line => line.StartsWith("#EXT-X-STREAM-INF", StringComparison.Ordinal)))
            {
                playlistUrl = ResolvePlaylistUrl(firstUri!, playlistUrl);
                continue;
            }

            if (!lines.Any(line => line.StartsWith("#EXTINF", StringComparison.Ordinal)))
                return false;

            var keyLine = lines.FirstOrDefault(line =>
                line.StartsWith("#EXT-X-KEY", StringComparison.Ordinal)
                && !ContainsIgnoreCase(line, "METHOD=NONE")
            );
            var isEncrypted = keyLine is not null;
            if (keyLine is not null)
            {
                var keyUrl = ExtractHlsAttributeUrl(keyLine, playlistUrl);
                if (keyUrl is null)
                    return false;
                if (!keyUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    var key = await ReadMediaPrefixAsync(keyUrl, headers, cancellationToken);
                    if (
                        key is null
                        || key.Value.Length < 16
                        || LooksLikeErrorPayload(key.Value.Buffer)
                    )
                        return false;
                }
            }

            var mapLine = lines.FirstOrDefault(line =>
                line.StartsWith("#EXT-X-MAP", StringComparison.Ordinal)
            );
            if (mapLine is not null)
            {
                var mapUrl = ExtractHlsAttributeUrl(mapLine, playlistUrl);
                if (mapUrl is null)
                    return false;

                var map = await ReadMediaPrefixAsync(mapUrl, headers, cancellationToken);
                if (map is null || !IsMediaPayload(map.Value.Buffer, map.Value.Length))
                    return false;
            }

            var segmentUrl = ResolvePlaylistUrl(firstUri!, playlistUrl);
            var segment = await ReadMediaPrefixAsync(segmentUrl, headers, cancellationToken);
            if (segment is null)
                return false;

            return isEncrypted
                ? segment.Value.Length >= 16 && !LooksLikeErrorPayload(segment.Value.Buffer)
                : IsMediaPayload(segment.Value.Buffer, segment.Value.Length);
        }

        return false;
    }

    private async ValueTask<(byte[] Buffer, int Length)?> ReadMediaPrefixAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Range = new RangeHeaderValue(0, 1023);
        foreach (var header in headers)
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);

        using var response = await _http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );
        if (!response.IsSuccessStatusCode)
            return null;

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[1024];
        var length = 0;
        while (length < buffer.Length)
        {
            var read = await stream.ReadAsync(
                buffer,
                length,
                buffer.Length - length,
                cancellationToken
            );
            if (read == 0)
                break;
            length += read;
        }

        return (buffer, length);
    }

    private static bool IsMediaPayload(byte[] buffer, int length)
    {
        if (length < 4 || LooksLikeErrorPayload(buffer))
            return false;

        if (HasBytes(buffer, length, 0, 0x1A, 0x45, 0xDF, 0xA3))
            return true;
        if (HasAscii(buffer, length, 0, "OggS") || HasAscii(buffer, length, 0, "FLV"))
            return true;
        if (HasAscii(buffer, length, 0, "ID3"))
            return true;
        if (HasBytes(buffer, length, 0, 0x00, 0x00, 0x01, 0xBA))
            return true;
        if (length >= 2 && buffer[0] == 0xFF && (buffer[1] & 0xF0) == 0xF0)
            return true;

        foreach (var boxType in new[] { "ftyp", "styp", "moof", "mdat" })
        {
            if (HasAscii(buffer, length, 4, boxType))
                return true;
        }

        for (var offset = 0; offset < Math.Min(188, length); offset++)
        {
            if (
                buffer[offset] == 0x47
                && offset + 188 < length
                && buffer[offset + 188] == 0x47
                && (offset + 376 >= length || buffer[offset + 376] == 0x47)
            )
                return true;
        }

        return false;
    }

    private static bool LooksLikeErrorPayload(byte[] buffer) =>
        HasAsciiIgnoreCase(buffer, "<!doctype")
        || HasAsciiIgnoreCase(buffer, "<html")
        || HasAsciiIgnoreCase(buffer, "<?xml")
        || HasBytes(buffer, buffer.Length, 0, 0x89, 0x50, 0x4E, 0x47)
        || HasAscii(buffer, buffer.Length, 0, "GIF8")
        || HasBytes(buffer, buffer.Length, 0, 0xFF, 0xD8, 0xFF)
        || (
            HasAscii(buffer, buffer.Length, 0, "RIFF") && HasAscii(buffer, buffer.Length, 8, "WEBP")
        );

    private static bool HasAscii(byte[] buffer, int length, int offset, string value)
    {
        if (offset < 0 || offset + value.Length > length)
            return false;

        for (var index = 0; index < value.Length; index++)
        {
            if (buffer[offset + index] != value[index])
                return false;
        }

        return true;
    }

    private static bool HasAsciiIgnoreCase(byte[] buffer, string value)
    {
        if (value.Length > buffer.Length)
            return false;

        for (var index = 0; index < value.Length; index++)
        {
            var character = (char)buffer[index];
            if (char.ToLowerInvariant(character) != value[index])
                return false;
        }

        return true;
    }

    private static bool HasBytes(byte[] buffer, int length, int offset, params byte[] values)
    {
        if (offset < 0 || offset + values.Length > length)
            return false;

        for (var index = 0; index < values.Length; index++)
        {
            if (buffer[offset + index] != values[index])
                return false;
        }

        return true;
    }

    private static string? ExtractHlsAttributeUrl(string line, string playlistUrl)
    {
        var value = _hlsUriAttributeRegex.Match(line).Groups["uri"].Value;
        return string.IsNullOrWhiteSpace(value) ? null : ResolvePlaylistUrl(value, playlistUrl);
    }

    private static string ResolvePlaylistUrl(string value, string playlistUrl) =>
        new Uri(new Uri(playlistUrl), WebUtility.HtmlDecode(value)).AbsoluteUri;

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static string GetFileType(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return "mp4";

        var extension = Path.GetExtension(uri.AbsolutePath).TrimStart('.');
        return string.IsNullOrWhiteSpace(extension) ? "mp4" : extension;
    }

    private async ValueTask<List<VideoSource>> FetchSourcesFromPageAsync(
        string url,
        VideoServer server,
        string referer,
        CancellationToken cancellationToken
    )
    {
        var body = await _http.ExecuteAsync(
            url,
            new Dictionary<string, string>
            {
                ["Referer"] = referer,
                ["User-Agent"] = Http.ChromeUserAgent(),
            },
            cancellationToken
        );

        if (body.TrimStart().StartsWith("#EXTM3U"))
            return BuildHlsSources(url, server, referer);

        var m3u8 = _m3u8Regex.Match(body).Value;
        if (string.IsNullOrWhiteSpace(m3u8))
            return [];

        return BuildHlsSources(m3u8, server, referer);
    }

    private async ValueTask<List<VideoSource>> ExtractFromMewcdnAsync(
        string embedUrl,
        VideoServer server,
        CancellationToken cancellationToken
    )
    {
        // The mewcdn player carries its m3u8 base64-encoded in the URL
        // fragment; the player page maps origin CDN hosts to proxy hosts.
        var fragment = Before(After(embedUrl, "#"), "#");
        if (string.IsNullOrWhiteSpace(fragment))
            return [];

        string rawM3u8;
        try
        {
            rawM3u8 = Encoding.UTF8.GetString(Convert.FromBase64String(PadBase64(fragment))).Trim();
        }
        catch (FormatException)
        {
            return [];
        }

        if (!rawM3u8.StartsWith("http"))
            return [];

        var pageBody = await _http.ExecuteAsync(
            embedUrl,
            new Dictionary<string, string>
            {
                ["Referer"] = $"{BaseUrl}/",
                ["User-Agent"] = Http.ChromeUserAgent(),
            },
            cancellationToken
        );

        var m3u8 = ApplyHostMap(rawM3u8, ParseHostMap(pageBody));
        return BuildHlsSources(m3u8, server, "https://mewcdn.online/", "https://mewcdn.online");
    }

    private List<VideoSource> BuildHlsSources(
        string m3u8Url,
        VideoServer server,
        string referer,
        string? origin = null
    ) =>
        [
            new VideoSource
            {
                Title = server.Name,
                Resolution = "Multi Quality",
                VideoUrl = m3u8Url,
                Format = VideoType.M3u8,
                FileType = "m3u8",
                Headers = BuildPlaybackHeaders(origin ?? referer.TrimEnd('/'), referer),
                VideoServer = server,
            },
        ];

    private static Dictionary<string, string> BuildPlaybackHeaders(
        string origin,
        string? referer = null
    ) =>
        new()
        {
            ["Accept"] = "*/*",
            ["User-Agent"] = Http.ChromeUserAgent(),
            ["Referer"] = referer ?? $"{origin}/",
            ["Origin"] = origin,
        };

    private async ValueTask<JsonObject?> TryGetJsonAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var body = await _http.ExecuteAsync(url, headers, cancellationToken);
            return JsonNode.Parse(body) as JsonObject;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static string? GetStreamType(string embedUrl)
    {
        if (!Uri.TryCreate(embedUrl, UriKind.Absolute, out var uri))
            return null;

        var lastSegment = uri.AbsolutePath.TrimEnd('/').Split('/').LastOrDefault();
        return lastSegment is "sub" or "dub" ? lastSegment : null;
    }

    private static string? GetSourceSelector(string embedUrl)
    {
        if (!Uri.TryCreate(embedUrl, UriKind.Absolute, out var uri))
            return null;

        foreach (var part in uri.Query.TrimStart('?').Split('&'))
        {
            var values = part.Split(new[] { '=' }, 2);
            if (values.Length != 2 || values[0] != "s")
                continue;

            var value = WebUtility.UrlDecode(values[1]);
            var sanitized = new string(
                value
                    .Where(character =>
                        character is >= 'a' and <= 'z'
                        || character is >= 'A' and <= 'Z'
                        || character is >= '0' and <= '9'
                        || character is '_' or '-'
                    )
                    .ToArray()
            );
            return string.IsNullOrWhiteSpace(sanitized) ? null : sanitized;
        }

        return null;
    }

    private static Dictionary<string, string> ParseHostMap(string html)
    {
        var map = new Dictionary<string, string>();
        var mapMatch = _hostMapRegex.Match(html);
        if (!mapMatch.Success)
            return map;

        foreach (Match entry in _hostEntryRegex.Matches(mapMatch.Groups["map"].Value))
            map[entry.Groups["origin"].Value] = entry.Groups["proxy"].Value;

        return map;
    }

    private static string ApplyHostMap(string url, Dictionary<string, string> hostMap)
    {
        foreach (var pair in hostMap)
        {
            if (url.Contains(pair.Key))
                return url.Replace(pair.Key, pair.Value);
        }

        return url;
    }

    private static string PadBase64(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        return (base64.Length % 4) switch
        {
            2 => base64 + "==",
            3 => base64 + "=",
            _ => base64,
        };
    }

    #endregion

    private List<IAnimeInfo> ParseAnimeResponse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return [];

        var document = Html.Parse(response!);
        var nodes = document.DocumentNode.SelectNodes(
            "//div[contains(@class, 'ani') and contains(@class, 'items')]/div[contains(@class, 'item')]"
        );
        if (nodes is null)
            return [];

        return nodes.Select(ParseAnimeCard).Where(x => x is not null).Cast<IAnimeInfo>().ToList();
    }

    private AnimeInfo? ParseAnimeCard(HtmlNode node)
    {
        var linkNode = node.SelectSingleNode(".//a[contains(@class, 'name')]");
        var href = Before(linkNode?.GetAttributeValue("href", string.Empty), "?");
        if (string.IsNullOrWhiteSpace(href))
            return null;

        var path = _episodeUrlSuffixRegex.Replace(href!, string.Empty);
        var imageNode = node.SelectSingleNode(".//div[contains(@class, 'poster')]//img");

        return new AnimeInfo
        {
            Id = path,
            Site = Site,
            Title = GetTitle(linkNode),
            OtherNames = linkNode?.GetAttributeValue("data-jp", string.Empty),
            Image = DefaultIfBlank(
                imageNode?.GetAttributeValue("data-src", string.Empty),
                imageNode?.GetAttributeValue("src", string.Empty)
            ),
            Link = $"{BaseUrl}{path}",
        };
    }

    private async ValueTask<string> ResolveAnimeIdAsync(
        string animePath,
        CancellationToken cancellationToken
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}{animePath}",
            BuildHeaders(BaseUrl),
            cancellationToken
        );
        var document = Html.Parse(response);
        return document
                .DocumentNode.SelectSingleNode("//*[@data-id]")
                ?.GetAttributeValue("data-id", string.Empty)
            ?? document
                .DocumentNode.SelectSingleNode("//*[@data-tip]")
                ?.GetAttributeValue("data-tip", string.Empty)
            ?? string.Empty;
    }

    private Episode ParseEpisode(HtmlNode node, string animePath)
    {
        var title = node.ParentNode?.GetAttributeValue("title", string.Empty) ?? string.Empty;
        var numberString = node.GetAttributeValue("data-num", "0");
        var number = float.TryParse(
            numberString,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var parsed
        )
            ? parsed
            : 0;
        var ids = node.GetAttributeValue("data-ids", string.Empty);
        var sub = node.GetAttributeValue("data-sub", string.Empty) == "1" ? "Sub" : string.Empty;
        var dub = node.GetAttributeValue("data-dub", string.Empty) == "1" ? "Dub" : string.Empty;
        var softSub = _softSubRegex.IsMatch(title) ? "SoftSub" : string.Empty;
        var episodeName =
            node.ParentNode?.SelectSingleNode(".//span[contains(@class, 'd-title')]")
                ?.InnerText.Trim()
            ?? string.Empty;
        var malId = node.GetAttributeValue("data-mal", string.Empty);
        var slug = node.GetAttributeValue("data-slug", string.Empty);
        var timestamp = node.GetAttributeValue("data-timestamp", string.Empty);
        var epUrl = $"{_episodeUrlSuffixRegex.Replace(animePath, string.Empty)}/ep-{numberString}";

        var id = new StringBuilder();
        id.Append(ids);
        id.Append("&epurl=");
        id.Append(epUrl);
        if (!string.IsNullOrWhiteSpace(malId))
            id.Append("&mal=").Append(malId);
        if (!string.IsNullOrWhiteSpace(slug))
            id.Append("&slug=").Append(slug);
        if (!string.IsNullOrWhiteSpace(timestamp))
            id.Append("&ts=").Append(timestamp);

        return new Episode
        {
            Id = id.ToString(),
            Number = number,
            Name =
                $"Episode {numberString}"
                + (
                    string.IsNullOrWhiteSpace(episodeName)
                    || episodeName == $"Episode {numberString}"
                        ? string.Empty
                        : $": {episodeName}"
                ),
            Description = string.Join(
                ", ",
                new[] { sub, softSub, dub }.Where(x => !string.IsNullOrWhiteSpace(x))
            ),
            Link = epUrl,
        };
    }

    private async ValueTask<VideoServer?> GetVideoServerAsync(
        HtmlNode node,
        string referer,
        CancellationToken cancellationToken
    )
    {
        var serverId = node.GetAttributeValue("data-link-id", string.Empty);
        if (string.IsNullOrWhiteSpace(serverId))
            return null;

        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/ajax/server?get={serverId}",
            BuildAjaxHeaders(referer),
            cancellationToken
        );
        var embed = JsonNode.Parse(response)?["result"]?["url"]?.ToString();
        if (string.IsNullOrWhiteSpace(embed))
            return null;

        var label = node.ParentNode?.ParentNode?.SelectSingleNode(".//label")?.InnerText.Trim();
        var name = node.InnerText.Trim();
        if (!string.IsNullOrWhiteSpace(label))
            name = $"{NormalizeTypeLabel(label!)} - {name}";

        return new VideoServer
        {
            Name = name,
            Embed = new FileUrl(NormalizeUrl(embed!, BaseUrl))
            {
                Headers = new Dictionary<string, string> { ["Referer"] = referer },
            },
        };
    }

    private static IEnumerable<string> ExtractVideoUrls(JsonNode? sources)
    {
        if (sources is JsonObject sourceObject)
        {
            var file = sourceObject["file"]?.ToString();
            if (!string.IsNullOrWhiteSpace(file))
                yield return file!;
            yield break;
        }

        if (sources is JsonArray sourceArray)
        {
            foreach (var source in sourceArray)
            {
                if (source is JsonObject obj)
                {
                    var file = obj["file"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(file))
                        yield return file!;
                }
                if (source is JsonValue)
                {
                    var file = source.ToString();
                    if (!string.IsNullOrWhiteSpace(file))
                        yield return file;
                }
            }
            yield break;
        }

        if (sources is JsonValue)
        {
            var file = sources.ToString();
            if (!string.IsNullOrWhiteSpace(file))
                yield return file;
        }
    }

    private static List<Subtitle> ParsePlayerSubtitles(JsonArray? tracks)
    {
        if (tracks is null)
            return [];

        return tracks
            .OfType<JsonObject>()
            .Where(track =>
                string.Equals(
                    track["kind"]?.ToString(),
                    "captions",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .Select(track => new Subtitle(
                track["file"]?.ToString() ?? string.Empty,
                track["label"]?.ToString() ?? "Subtitle"
            ))
            .Where(track => !string.IsNullOrWhiteSpace(track.Url))
            .ToList();
    }

    private static string NormalizeAnimePath(string id)
    {
        var value = Before(id, "#");
        if (string.IsNullOrWhiteSpace(value))
            value = id;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            value = uri.AbsolutePath;

        value = Before(value, "?");
        value = _episodeUrlSuffixRegex.Replace(value, string.Empty);
        return value.StartsWith("/") ? value : "/" + value.Trim('/');
    }

    private static string ExtractFragment(string value) =>
        value.IndexOf('#') >= 0 ? After(value, "#") : string.Empty;

    private static string GetTitle(HtmlNode? node)
    {
        if (node is null)
            return string.Empty;

        var english = WebUtility.HtmlDecode(node.InnerText).Trim();
        var japanese = node.GetAttributeValue("data-jp", string.Empty).Trim();
        return DefaultIfBlank(english, japanese);
    }

    private static string? SelectMetaValue(HtmlDocument document, string label)
    {
        var node = document.DocumentNode.SelectSingleNode(
            $"//div[contains(@class, 'meta')]/div[contains(., '{label}')]/span"
        );
        return node?.InnerText.Trim();
    }

    private static string NormalizeTypeLabel(string label) =>
        label.ToUpperInvariant() switch
        {
            "SUB" => "Sub",
            "H-SUB" => "H-Sub",
            "DUB" => "Dub",
            "A-DUB" => "A-Dub",
            _ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(label.ToLowerInvariant()),
        };

    private static Dictionary<string, string> BuildHeaders(string referer) =>
        new() { ["Referer"] = referer.EndsWith("/") ? referer : referer + "/" };

    private static Dictionary<string, string> BuildAjaxHeaders(string referer) =>
        new()
        {
            ["Accept"] = "application/json, text/javascript, */*; q=0.01",
            ["Referer"] = referer,
            ["X-Requested-With"] = "XMLHttpRequest",
        };

    /// <summary>
    /// Encodes an Anikoto-theme VRF token.
    /// </summary>
    public static string VrfEncrypt(string input)
    {
        var vrf = Exchange(input, "AP6GeR8H0lwUz1", "UAz8Gwl10P6ReH");
        vrf = Rc4Base64("ItFKjuWokn4ZpB", vrf);
        vrf = Rc4Base64("fOyt97QWFB3", vrf);
        vrf = Exchange(vrf, "1majSlPQd2M5", "da1l2jSmP5QM");
        vrf = Exchange(vrf, "CPYvHj09Au3", "0jHA9CPYu3v");
        vrf = new string(vrf.Reverse().ToArray());
        vrf = Rc4Base64("736y1uTJpBLUX", vrf);
        vrf = Base64UrlEncode(Encoding.UTF8.GetBytes(vrf));
        return Uri.EscapeDataString(vrf);
    }

    private static string Exchange(string input, string key1, string key2)
    {
        var chars = input.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var index = key1.IndexOf(chars[i]);
            if (index >= 0)
                chars[i] = key2[index];
        }

        return new string(chars);
    }

    private static string Rc4Base64(string key, string input) =>
        Base64UrlEncode(Rc4(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(input)));

    private static byte[] Rc4(byte[] key, byte[] input)
    {
        var s = Enumerable.Range(0, 256).Select(x => (byte)x).ToArray();
        var j = 0;
        for (var i = 0; i < 256; i++)
        {
            j = (j + s[i] + key[i % key.Length]) & 255;
            var temp = s[i];
            s[i] = s[j];
            s[j] = temp;
        }

        var output = new byte[input.Length];
        var a = 0;
        j = 0;
        for (var index = 0; index < input.Length; index++)
        {
            a = (a + 1) & 255;
            j = (j + s[a]) & 255;
            var temp = s[a];
            s[a] = s[j];
            s[j] = temp;
            output[index] = (byte)(input[index] ^ s[(s[a] + s[j]) & 255]);
        }

        return output;
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_');

    private static string NormalizeUrl(string url, string baseUrl)
    {
        if (url.StartsWith("//"))
            return "https:" + url;

        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            return url;

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            return url;

        return url.StartsWith("/")
            ? uri.GetLeftPart(UriPartial.Authority) + url
            : uri.GetLeftPart(UriPartial.Authority) + "/" + url;
    }

    private static string DefaultIfBlank(string? value, string? fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback ?? string.Empty : value!;

    private static bool ContainsIgnoreCase(string value, string needle) =>
        value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

    private static string Before(string? value, string marker)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var index = value.IndexOf(marker, StringComparison.Ordinal);
        return index < 0 ? value : value.Substring(0, index);
    }

    private static string After(string value, string marker)
    {
        var index = value.IndexOf(marker, StringComparison.Ordinal);
        return index < 0 ? string.Empty : value.Substring(index + marker.Length);
    }
}
