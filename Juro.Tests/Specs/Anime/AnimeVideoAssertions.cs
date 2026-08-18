using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core.Models.Videos;
using Juro.Core.Providers;
using Xunit;

namespace Juro.Tests.Specs.Anime;

internal static class AnimeVideoAssertions
{
    private const string BrowserUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) "
        + "Chrome/148.0.0.0 Safari/537.36";
    private static readonly HttpClient _http = new();

    public static async Task AssertPlayableVideoAsync(
        IAnimeProvider provider,
        string providerName,
        string query = "spy x family"
    )
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var results = await provider.SearchAsync(query, cancellationToken: cancellationToken);
        if (results.Count == 0)
            Assert.Fail($"{providerName} returned no search results for '{query}'.");

        var result =
            results.FirstOrDefault(item =>
                string.Equals(item.Title, query, StringComparison.OrdinalIgnoreCase)
            ) ?? results[0];
        var episodes = await provider.GetEpisodesAsync(
            result.Id,
            cancellationToken: cancellationToken
        );
        if (episodes.Count == 0)
            Assert.Fail($"{providerName} returned no episodes for '{result.Title}'.");

        var videoServers = await provider.GetVideoServersAsync(
            episodes[0].Id,
            cancellationToken: cancellationToken
        );
        if (videoServers.Count == 0)
            Assert.Fail($"{providerName} returned no video servers for '{episodes[0].Name}'.");

        await AssertAnyServerPlayableAsync(
            provider,
            videoServers,
            $"{providerName} / {result.Title} / {episodes[0].Name}",
            cancellationToken
        );
    }

    public static async Task AssertAnyServerPlayableAsync(
        IAnimeProvider provider,
        IEnumerable<VideoServer> videoServers,
        string context,
        CancellationToken cancellationToken
    )
    {
        var failures = new List<string>();
        foreach (var server in videoServers)
        {
            if (await IsServerPlayableAsync(provider, server, failures, cancellationToken))
                return;
        }

        if (
            failures.Count > 0
            && failures.All(x => x.Contains("403 (Forbidden)", StringComparison.Ordinal))
        )
        {
            throw new HttpRequestException(
                $"{context} playback is blocked with 403 (Forbidden). "
                    + string.Join("; ", failures)
            );
        }

        Assert.Fail(
            $"{context} did not expose a playable video source. " + string.Join("; ", failures)
        );
    }

    public static async Task AssertEveryServerPlayableAsync(
        IAnimeProvider provider,
        IEnumerable<VideoServer> videoServers,
        string context,
        CancellationToken cancellationToken
    )
    {
        var servers = videoServers.ToList();
        if (servers.Count == 0)
            Assert.Fail($"{context} returned no video servers.");

        foreach (var server in servers)
        {
            var failures = new List<string>();
            if (!await IsServerPlayableAsync(provider, server, failures, cancellationToken))
            {
                Assert.Fail(
                    $"{context} server '{server.Name}' did not expose a playable video source. "
                        + string.Join("; ", failures)
                );
            }
        }
    }

    private static async Task<bool> IsServerPlayableAsync(
        IAnimeProvider provider,
        VideoServer server,
        List<string> failures,
        CancellationToken cancellationToken
    )
    {
        List<VideoSource> videos;
        try
        {
            videos = await provider.GetVideosAsync(server, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            failures.Add($"{server.Name}: extraction failed ({exception.Message})");
            return false;
        }

        if (videos.Count == 0)
        {
            failures.Add($"{server.Name}: extraction returned no URLs");
            return false;
        }

        foreach (var video in videos)
        {
            var result = await ProbeAsync(video, cancellationToken);
            if (result.IsPlayable)
                return true;

            failures.Add(
                $"{server.Name} / {video.VideoUrl} / headers "
                    + $"{string.Join(", ", video.Headers.Select(x => $"{x.Key}={x.Value}"))}: "
                    + result.Reason
            );
        }

        return false;
    }

    internal static async Task<(bool IsPlayable, string Reason)> ProbeAsync(
        VideoSource source,
        CancellationToken cancellationToken
    ) => await ProbeAsync(source, _http, cancellationToken);

    internal static async Task<(bool IsPlayable, string Reason)> ProbeAsync(
        VideoSource source,
        HttpClient http,
        CancellationToken cancellationToken
    )
    {
        if (
            !Uri.TryCreate(source.VideoUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        )
            return (false, "URL is not an absolute HTTP(S) URL");

        try
        {
            if (
                source.Format is VideoType.M3u8 or VideoType.Hls
                || source.VideoUrl.Contains(".m3u8", StringComparison.OrdinalIgnoreCase)
            )
                return await ProbeHlsAsync(source, http, cancellationToken);

            if (
                source.Format == VideoType.Dash
                || source.VideoUrl.Contains(".mpd", StringComparison.OrdinalIgnoreCase)
            )
            {
                var manifest = await GetTextAsync(
                    http,
                    source.VideoUrl,
                    source.Headers,
                    cancellationToken
                );
                return manifest.Contains("<MPD", StringComparison.OrdinalIgnoreCase)
                    ? (true, string.Empty)
                    : (false, "response is not a DASH MPD manifest");
            }

            var prefix = await ReadPrefixAsync(
                http,
                source.VideoUrl,
                source.Headers,
                cancellationToken
            );
            return IsMediaPayload(prefix.Buffer, prefix.Length)
                ? (true, string.Empty)
                : (false, DescribeInvalidPayload(prefix));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return (false, $"request failed ({exception.Message})");
        }
    }

    private static async Task<(bool IsPlayable, string Reason)> ProbeHlsAsync(
        VideoSource source,
        HttpClient http,
        CancellationToken cancellationToken
    )
    {
        var playlistUrl = source.VideoUrl;
        for (var depth = 0; depth < 3; depth++)
        {
            var playlist = await GetTextAsync(http, playlistUrl, source.Headers, cancellationToken);
            if (!playlist.TrimStart().StartsWith("#EXTM3U", StringComparison.Ordinal))
                return (false, $"{playlistUrl} is not an HLS manifest");

            var lines = playlist
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToList();
            var firstUri = lines.FirstOrDefault(line => !line.StartsWith("#"));
            if (string.IsNullOrWhiteSpace(firstUri))
                return (false, $"{playlistUrl} contains no media URI");

            if (lines.Any(line => line.StartsWith("#EXT-X-STREAM-INF", StringComparison.Ordinal)))
            {
                playlistUrl = ResolveUrl(playlistUrl, firstUri!);
                continue;
            }

            if (!lines.Any(line => line.StartsWith("#EXTINF", StringComparison.Ordinal)))
                return (false, $"{playlistUrl} is not a media playlist");

            var keyLine = lines.FirstOrDefault(line =>
                line.StartsWith("#EXT-X-KEY", StringComparison.Ordinal)
                && !line.Contains("METHOD=NONE", StringComparison.OrdinalIgnoreCase)
            );
            var encrypted = keyLine is not null;
            if (keyLine is not null)
            {
                var keyUrl = ExtractAttributeUrl(keyLine, playlistUrl);
                if (keyUrl is null)
                    return (false, $"{playlistUrl} has an encryption key without a URI");

                if (!keyUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    var key = await ReadPrefixAsync(
                        http,
                        keyUrl,
                        source.Headers,
                        cancellationToken
                    );
                    if (key.Length < 16 || LooksLikeErrorPayload(key.Buffer, key.Length))
                        return (false, $"{keyUrl} did not return a valid HLS key");
                }
            }

            var mapLine = lines.FirstOrDefault(line =>
                line.StartsWith("#EXT-X-MAP", StringComparison.Ordinal)
            );
            if (mapLine is not null)
            {
                var mapUrl = ExtractAttributeUrl(mapLine, playlistUrl);
                if (mapUrl is null)
                    return (false, $"{playlistUrl} has an initialization map without a URI");

                var map = await ReadPrefixAsync(http, mapUrl, source.Headers, cancellationToken);
                if (!IsMediaPayload(map.Buffer, map.Length))
                    return (false, DescribeInvalidPayload(map));
            }

            var segmentUrl = ResolveUrl(playlistUrl, firstUri!);
            var prefix = await ReadPrefixAsync(http, segmentUrl, source.Headers, cancellationToken);

            if (encrypted)
            {
                return prefix.Length >= 16 && !LooksLikeErrorPayload(prefix.Buffer, prefix.Length)
                    ? (true, string.Empty)
                    : (false, DescribeInvalidPayload(prefix));
            }

            return IsMediaPayload(prefix.Buffer, prefix.Length)
                ? (true, string.Empty)
                : (false, DescribeInvalidPayload(prefix));
        }

        return (false, "HLS playlists nested more than three levels");
    }

    private static async Task<string> GetTextAsync(
        HttpClient http,
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken
    )
    {
        using var request = CreateRequest(HttpMethod.Get, url, headers);
        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static async Task<(byte[] Buffer, int Length, string? ContentType)> ReadPrefixAsync(
        HttpClient http,
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken
    )
    {
        using var request = CreateRequest(HttpMethod.Get, url, headers);
        request.Headers.Range = new RangeHeaderValue(0, 1023);
        using var response = await http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[1024];
        var length = 0;
        while (length < buffer.Length)
        {
            var read = await stream.ReadAsync(
                buffer.AsMemory(length, buffer.Length - length),
                cancellationToken
            );
            if (read == 0)
                break;
            length += read;
        }

        return (buffer, length, response.Content.Headers.ContentType?.MediaType);
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string url,
        Dictionary<string, string> headers
    )
    {
        var request = new HttpRequestMessage(method, url);
        foreach (var header in headers)
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        if (!request.Headers.Contains("User-Agent"))
            request.Headers.TryAddWithoutValidation("User-Agent", BrowserUserAgent);
        return request;
    }

    private static bool IsMediaPayload(byte[] buffer, int length)
    {
        if (length < 4 || LooksLikeErrorPayload(buffer, length))
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

    private static bool LooksLikeErrorPayload(byte[] buffer, int length) =>
        HasAsciiIgnoreCase(buffer, length, "<!doctype")
        || HasAsciiIgnoreCase(buffer, length, "<html")
        || HasAsciiIgnoreCase(buffer, length, "<?xml")
        || HasBytes(buffer, length, 0, 0x89, 0x50, 0x4E, 0x47)
        || HasAscii(buffer, length, 0, "GIF8")
        || HasBytes(buffer, length, 0, 0xFF, 0xD8, 0xFF)
        || (HasAscii(buffer, length, 0, "RIFF") && HasAscii(buffer, length, 8, "WEBP"));

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

    private static bool HasAsciiIgnoreCase(byte[] buffer, int length, string value)
    {
        if (value.Length > length)
            return false;

        for (var index = 0; index < value.Length; index++)
        {
            if (char.ToLowerInvariant((char)buffer[index]) != value[index])
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

    private static string? ExtractAttributeUrl(string line, string playlistUrl)
    {
        const string marker = "URI=\"";
        var start = line.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return null;

        start += marker.Length;
        var end = line.IndexOf('"', start);
        return end < 0 ? null : ResolveUrl(playlistUrl, line.Substring(start, end - start));
    }

    private static string ResolveUrl(string baseUrl, string value) =>
        new Uri(new Uri(baseUrl), WebUtility.HtmlDecode(value)).AbsoluteUri;

    private static string DescribeInvalidPayload(
        (byte[] Buffer, int Length, string? ContentType) payload
    ) =>
        $"response is not recognized media (content-type {payload.ContentType ?? "unknown"}, "
        + $"{payload.Length} bytes, prefix {Convert.ToHexString(payload.Buffer, 0, Math.Min(12, payload.Length))})";
}
