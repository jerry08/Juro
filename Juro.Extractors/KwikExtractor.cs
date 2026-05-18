using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core;
using Juro.Core.Models.Videos;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;

namespace Juro.Extractors;

/// <summary>
/// Extractor for Kwik.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="KwikExtractor"/>.
/// </remarks>
public class KwikExtractor(IHttpClientFactory httpClientFactory) : IVideoExtractor
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private readonly string _host = "https://animepahe.com";

    //private readonly Regex _redirectRegex = new("<a href=\"(.+?)\" .+?>Redirect me</a>");
    private readonly Regex _redirectRegex = new(@"https://kwik\..+?/.*?/[A-Za-z0-9]+");
    private readonly Regex _paramRegex = new("""\(\"(\w+)\",\d+,\"(\w+)\",(\d+),(\d+),\d+\)""");
    private readonly Regex _urlRegex = new("action=\"(.+?)\"");
    private readonly Regex _tokenRegex = new("value=\"(.+?)\"");
    private readonly Regex _scriptRegex = new(
        @"<script[^>]*>(?<script>[\s\S]*?eval\(function[\s\S]*?)</script>",
        RegexOptions.IgnoreCase
    );
    private readonly Regex _hlsSourceRegex = new(
        @"source\s*=\s*\\?'(?<url>[^']+)",
        RegexOptions.IgnoreCase
    );

    /// <inheritdoc />
    public string ServerName => "Kwik";

    /// <summary>
    /// Initializes an instance of <see cref="KwikExtractor"/>.
    /// </summary>
    public KwikExtractor(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="KwikExtractor"/>.
    /// </summary>
    public KwikExtractor()
        : this(Http.ClientProvider) { }

    /// <inheritdoc />
    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default
    ) => await ExtractAsync(url, [], cancellationToken);

    /// <inheritdoc />
    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var host = headers.TryGetValue("Referer", out var referer) ? referer : _host;
        if (url.IndexOf("/e/", StringComparison.OrdinalIgnoreCase) >= 0)
            return await ExtractHlsAsync(url, host, cancellationToken);

        var noRedirectClient = _httpClientFactory.CreateClient();
        noRedirectClient.SetAllowAutoRedirect(false);

        var kwikLink = await ResolveKwikUrlAsync(noRedirectClient, url, host, cancellationToken);
        if (string.IsNullOrWhiteSpace(kwikLink))
            return [];

        var http = _httpClientFactory.CreateClient();
        using var kwikRequest = new HttpRequestMessage(HttpMethod.Get, kwikLink);
        kwikRequest.Headers.TryAddWithoutValidation("Origin", "https://kwik.cx");
        kwikRequest.Headers.TryAddWithoutValidation("Referer", "https://kwik.cx/");
        var kwikRes = await http.SendAsync(
            kwikRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );
        var text = await kwikRes.Content.ReadAsStringAsync(cancellationToken);
        var cookies = kwikRes.Headers.TryGetValues("set-cookie", out var cookieValues)
            ? string.Join("; ", cookieValues.Select(x => x.Split(';')[0]))
            : string.Empty;
        var match = _paramRegex.Match(text);
        if (!match.Success)
            return [];

        var groups = match.Groups.OfType<Group>().ToArray();
        var fullKey = groups[1].Value;
        var key = groups[2].Value;
        var v1 = groups[3].Value;
        var v2 = groups[4].Value;

        var decrypted = Decrypt(fullKey, key, int.Parse(v1), int.Parse(v2));
        var postUrl = _urlRegex
            .Match(decrypted)
            .Groups.OfType<Group>()
            .ElementAtOrDefault(1)
            ?.Value;
        var token = _tokenRegex
            .Match(decrypted)
            .Groups.OfType<Group>()
            .ElementAtOrDefault(1)
            ?.Value;
        if (string.IsNullOrWhiteSpace(postUrl) || string.IsNullOrWhiteSpace(token))
            return [];

        headers = new Dictionary<string, string>() { { "Referer", kwikLink } };
        if (!string.IsNullOrWhiteSpace(cookies))
            headers["Cookie"] = cookies!;

        var formContent = new FormUrlEncodedContent(
            new KeyValuePair<string?, string?>[] { new("_token", token) }
        );

        var request = new HttpRequestMessage(HttpMethod.Post, postUrl);
        for (var j = 0; j < headers.Count; j++)
            request.Headers.TryAddWithoutValidation(
                headers.ElementAt(j).Key,
                headers.ElementAt(j).Value
            );

        if (!request.Headers.Contains("User-Agent"))
        {
            request.Headers.Add("User-Agent", Http.ChromeUserAgent());
        }

        request.Content = formContent;

        http = _httpClientFactory.CreateClient();
        http.SetAllowAutoRedirect(false);

        var response2 = await http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );

        var mp4Url = response2.Headers.Location?.ToString();
        if (string.IsNullOrWhiteSpace(mp4Url))
            return [];

        return
        [
            new()
            {
                VideoUrl = mp4Url!,
                Format = VideoType.Container,
                FileType = "mp4",
            },
        ];
    }

    private async ValueTask<List<VideoSource>> ExtractHlsAsync(
        string url,
        string referer,
        CancellationToken cancellationToken
    )
    {
        var http = _httpClientFactory.CreateClient();
        var response = await http.ExecuteAsync(
            url,
            new Dictionary<string, string>
            {
                ["Origin"] = "https://kwik.cx",
                ["Referer"] = referer,
            },
            cancellationToken
        );
        var script = _scriptRegex.Match(response).Groups["script"].Value;
        if (string.IsNullOrWhiteSpace(script))
            return [];

        var unpacked = JsUnpacker.UnpackAndCombine(script);
        var videoUrl = _hlsSourceRegex.Match(unpacked).Groups["url"].Value.Replace("\\/", "/");
        if (string.IsNullOrWhiteSpace(videoUrl))
            return [];

        return
        [
            new VideoSource
            {
                VideoUrl = videoUrl,
                Format = VideoType.M3u8,
                FileType = "m3u8",
                Resolution = "Multi Quality",
                Headers = new Dictionary<string, string>
                {
                    ["Origin"] = "https://kwik.cx",
                    ["Referer"] = "https://kwik.cx/",
                },
            },
        ];
    }

    private async ValueTask<string> ResolveKwikUrlAsync(
        HttpClient noRedirectClient,
        string paheUrl,
        string referer,
        CancellationToken cancellationToken
    )
    {
        var redirectUrl = paheUrl.EndsWith("/i") ? paheUrl : paheUrl.TrimEnd('/') + "/i";
        using var request = new HttpRequestMessage(HttpMethod.Get, redirectUrl);
        request.Headers.TryAddWithoutValidation("Referer", referer);

        using var response = await noRedirectClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );
        var location = response.Headers.Location?.ToString();
        if (!string.IsNullOrWhiteSpace(location))
            return NormalizeKwikUrl(location!);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return _redirectRegex.Match(body).Value;
    }

    private static string NormalizeKwikUrl(string location)
    {
        if (location.StartsWith("//"))
            return "https:" + location;

        var embedded = location.LastIndexOf("https://", StringComparison.OrdinalIgnoreCase);
        if (embedded > 0)
            return location.Substring(embedded);

        return location;
    }

    private readonly string _map =
        "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ+/";

    private int GetString(string content, int s1)
    {
        var s2 = 10;
        var slice = _map.Substring(0, s2);
        double acc = 0;

        var reversedMap = content.Reverse();

        for (var i = 0; i < reversedMap.Length; i++)
        {
            var c = reversedMap[i];
            acc += (char.IsDigit(c) ? int.Parse(c.ToString()) : 0) * Math.Pow(s1, i);
        }

        var k = "";

        while (acc > 0)
        {
            k = slice[(int)(acc % s2)] + k;
            acc = (acc - (acc % s2)) / s2;
        }

        return int.TryParse(k, out var l) ? l : 0;
    }

    private string Decrypt(string fullKey, string key, int v1, int v2)
    {
        var keyMap = key.Select((value, index) => new { value, index })
            .ToDictionary(x => x.value, x => x.index);
        var result = new StringBuilder();
        var i = 0;
        var marker = key[v2];

        while (i < fullKey.Length)
        {
            var nextIndex = fullKey.IndexOf(marker, i);
            if (nextIndex == -1)
                break;

            var decodedChar = new StringBuilder();
            for (var j = i; j < nextIndex; j++)
            {
                if (!keyMap.TryGetValue(fullKey[j], out var mapped))
                    return result.ToString();

                decodedChar.Append(mapped);
            }

            i = nextIndex + 1;
            if (!TryParseRadix(decodedChar.ToString(), v2, out var value))
                break;

            result.Append((char)(value - v1));
        }

        return result.ToString();
    }

    private static bool TryParseRadix(string value, int radix, out int result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value) || radix < 2)
            return false;

        foreach (var ch in value)
        {
            var digit = ch >= '0' && ch <= '9' ? ch - '0' : char.ToLowerInvariant(ch) - 'a' + 10;
            if (digit < 0 || digit >= radix)
                return false;
            checked
            {
                result = result * radix + digit;
            }
        }

        return true;
    }
}
