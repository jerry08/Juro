using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models.Videos;
using Juro.Utils;
using Juro.Utils.Extensions;

namespace Juro.Extractors;

public class Mp4upload : IVideoExtractor
{
    private readonly Func<HttpClient> _httpClientProvider;

    public string ServerName => "Mp4upload";

    public Mp4upload(Func<HttpClient> httpClientProvider)
    {
        _httpClientProvider = httpClientProvider;
    }

    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var http = _httpClientProvider();

        var headers = new Dictionary<string, string>()
        {
            ["Referer"] = "https://mp4upload.com/"
        };

        var response = await http.ExecuteAsync(url, headers, cancellationToken);

        var packed = response.SubstringAfter("eval(function(p,a,c,k,e,d)")
            .Split(new[] { "</script>" }, StringSplitOptions.None)[0];

        var unpacked = JsUnpacker.UnpackAndCombine($"eval(function(p,a,c,k,e,d){packed}");

        if (string.IsNullOrEmpty(unpacked))
            return new();

        var videoUrl = unpacked.SubstringAfter("player.src(\"")
            .Split(new[] { "\");" }, StringSplitOptions.None)[0];

        return new List<VideoSource>
        {
            new()
            {
                Format = VideoType.Container,
                VideoUrl = videoUrl,
                Resolution = "Default Quality",
                Headers = headers
            }
        };
    }
}