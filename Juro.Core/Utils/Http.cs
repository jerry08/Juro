using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace Juro.Core.Utils;

internal static class Http
{
    /// <summary>
    /// Chrome major version advertised by <see cref="ChromeUserAgent"/> and the
    /// <c>Sec-Ch-Ua</c> client hints. Keep both in sync — WAFs (e.g. Cloudflare
    /// custom rules on miruro.tv and mangadex.org) flag mismatched brand claims.
    /// </summary>
    private const string ChromeMajorVersion = "148";

    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) "
        + $"Chrome/{ChromeMajorVersion}.0.0.0 Safari/537.36";

    internal const string SecChUa =
        $"\"Chromium\";v=\"{ChromeMajorVersion}\", \"Not_A Brand\";v=\"24\", \"Google Chrome\";v=\"{ChromeMajorVersion}\"";

    public static Func<HttpClient> ClientProvider =>
        () =>
        {
            var handler = new HttpClientHandler();

            if (handler.SupportsAutomaticDecompression)
            {
#if NETCOREAPP
                handler.AutomaticDecompression = DecompressionMethods.All;
#else
                handler.AutomaticDecompression =
                    DecompressionMethods.GZip | DecompressionMethods.Deflate;
#endif
            }

            var httpClient = new HttpClient(handler, true);

#if NETCOREAPP
            // Prefer HTTP/2 like real browsers do (with ALPN fallback to 1.1).
            // Some WAFs (e.g. miruro.tv) hard-403 HTTP/1.1 API requests.
            httpClient.DefaultRequestVersion = HttpVersion.Version20;
            httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
#endif

            if (!httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", ChromeUserAgent());
            }

            return httpClient;
        };

    /// <summary>
    /// Returns a User-Agent matching a current desktop Chrome release.
    /// Chrome froze the build/patch components at <c>0.0.0</c> (UA reduction),
    /// so a fixed modern value looks more legitimate to WAFs than randomized
    /// version/OS combinations that never shipped together.
    /// </summary>
    public static string ChromeUserAgent() => UserAgent;

    /// <summary>
    /// Browser-shaped headers for an XHR/fetch-style API request, mirroring what
    /// desktop Chrome sends on a CORS <c>fetch()</c>. Several sites now enforce
    /// this header shape via WAF custom rules and 403/400 anything else.
    /// </summary>
    /// <param name="origin">Origin of the requesting page, e.g. <c>https://example.org</c>.</param>
    /// <param name="referer">Referer to send; defaults to <paramref name="origin"/> + "/".</param>
    /// <param name="sameOrigin">Whether the API shares the page's origin (<c>same-origin</c>) or only its site (<c>same-site</c>).</param>
    public static Dictionary<string, string> ApiFingerprintHeaders(
        string origin,
        string? referer = null,
        bool sameOrigin = true
    ) =>
        new()
        {
            ["Accept"] = "*/*",
            ["Accept-Language"] = "en-US,en;q=0.9",
            ["User-Agent"] = ChromeUserAgent(),
            ["Sec-Ch-Ua"] = SecChUa,
            ["Sec-Ch-Ua-Mobile"] = "?0",
            ["Sec-Ch-Ua-Platform"] = "\"Windows\"",
            ["Sec-Fetch-Dest"] = "empty",
            ["Sec-Fetch-Mode"] = "cors",
            ["Sec-Fetch-Site"] = sameOrigin ? "same-origin" : "same-site",
            ["Origin"] = origin,
            ["Referer"] = referer ?? $"{origin}/",
        };

    /// <summary>
    /// Browser-shaped headers for a top-level document navigation, mirroring
    /// what desktop Chrome sends when loading a page in a fresh tab.
    /// </summary>
    public static Dictionary<string, string> NavigationFingerprintHeaders(string? referer = null)
    {
        var headers = new Dictionary<string, string>
        {
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            ["Accept-Language"] = "en-US,en;q=0.9",
            ["User-Agent"] = ChromeUserAgent(),
            ["Sec-Ch-Ua"] = SecChUa,
            ["Sec-Ch-Ua-Mobile"] = "?0",
            ["Sec-Ch-Ua-Platform"] = "\"Windows\"",
            ["Sec-Fetch-Dest"] = "document",
            ["Sec-Fetch-Mode"] = "navigate",
            ["Sec-Fetch-Site"] = referer is null ? "none" : "same-origin",
        };

        if (referer is not null)
            headers["Referer"] = referer;

        return headers;
    }
}
