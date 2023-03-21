using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models.Videos;
using Juro.Utils;
using Juro.Utils.Extensions;

namespace Juro.Extractors;

public class Kwik : IVideoExtractor
{
    private readonly HttpClient _http;

    private readonly string _host = "https://animepahe.com";

    private readonly Regex _redirectRegex = new("<a href=\"(.+?)\" .+?>Redirect me</a>");
    private readonly Regex _paramRegex = new("""\(\"(\w+)\",\d+,\"(\w+)\",(\d+),(\d+),(\d+)\)""");
    private readonly Regex _urlRegex = new("action=\"(.+?)\"");
    private readonly Regex _tokenRegex = new("value=\"(.+?)\"");

    public string ServerName => "Kwik";

    public Kwik(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.ExecuteAsync(
            url,
            new Dictionary<string, string>()
            {
                { "Referer", _host }
            },
            cancellationToken
        );

        var kwikLink = _redirectRegex.Match(response).Groups[1].Value;

        var kwikRes = await _http.GetAsync(kwikLink, cancellationToken);
        var text = await kwikRes.Content.ReadAsStringAsync(cancellationToken);
        var cookies = kwikRes.Headers.GetValues("set-cookie").ElementAt(0);
        var groups = _paramRegex.Match(text).Groups.OfType<Group>().ToArray();
        var fullKey = groups[1].Value;
        var key = groups[2].Value;
        var v1 = groups[3].Value;
        var v2 = groups[4].Value;

        var decrypted = Decrypt(fullKey, key, int.Parse(v1), int.Parse(v2));
        var postUrl = _urlRegex.Match(decrypted).Groups.OfType<Group>().ToArray()[1].Value;
        var token = _tokenRegex.Match(decrypted).Groups.OfType<Group>().ToArray()[1].Value;

        var http = new HttpClient(new HttpClientHandler()
        {
            AllowAutoRedirect = false,
        });

        var headers = new Dictionary<string, string>()
        {
            { "Referer", kwikLink },
            { "Cookie", cookies }
        };

        var formContent = new FormUrlEncodedContent(new KeyValuePair<string?, string?>[]
        {
            new("_token", token)
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, postUrl);
        for (var j = 0; j < headers.Count; j++)
            request.Headers.TryAddWithoutValidation(headers.ElementAt(j).Key, headers.ElementAt(j).Value);

        if (!request.Headers.Contains("User-Agent"))
        {
            request.Headers.Add(
                "User-Agent",
                Http.ChromeUserAgent()
            );
        }

        request.Content = formContent;

        using var response2 = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead
, cancellationToken);

        var mp4Url = response2.Headers.Location!;

        return new()
        {
            new()
            {
                VideoUrl = mp4Url.ToString(),
                Format = VideoType.Container,
                FileType = "mp4"
            }
        };
    }

    private readonly string _map = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ+/";

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
        var r = "";
        for (var i = 0; i < fullKey.Length; i++)
        {
            var s = "";
            while (fullKey[i] != key[v2])
            {
                s += fullKey[i];
                i++;
            }

            for (var j = 0; j < key.Length; j++)
            {
                s = s.Replace(key[j].ToString(), j.ToString());
            }

            r += (char)(GetString(s, v2) - v1);
        }

        return r;
    }
}