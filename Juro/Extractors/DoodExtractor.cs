using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models.Videos;
using Juro.Utils.Extensions;

namespace Juro.Extractors;

public class DoodExtractor : IVideoExtractor
{
    private readonly Func<HttpClient> _httpClientProvider;

    public string ServerName => "Dood";

    public DoodExtractor(Func<HttpClient> httpClientProvider)
    {
        _httpClientProvider = httpClientProvider;
    }

    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var http = _httpClientProvider();

        var response = await http.ExecuteAsync(url, cancellationToken);

        if (!response.Contains("'/pass_md5/"))
            return new();

        var doodTld = url.SubstringAfter("https://dood.").SubstringBefore("/");
        var md5 = response.SubstringAfter("'/pass_md5/").SubstringBefore("',");
        var token = md5.Split(new[] { "/" }, StringSplitOptions.None).LastOrDefault();
        var randomString = RandomString();
        var expiry = DateTime.Now.CurrentTimeMillis();

        var videoUrlStart = await http.ExecuteAsync(
            $"https://dood.{doodTld}/pass_md5/{md5}",
            new()
            {
                ["Referer"] = url
            },
            cancellationToken
        );

        var videoUrl = $"{videoUrlStart}{randomString}?token={token}&expiry={expiry}";

        return new()
        {
            new()
            {
                Format = VideoType.Container,
                VideoUrl = videoUrl,
                Resolution = "Default Quality",
                Headers = new()
                {
                    ["User-Agent"] = "Anistream",
                    ["Referer"] = $"https://dood.{doodTld}"
                }
            }
        };
    }

    private static Random random = new();

    private static string RandomString(int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}