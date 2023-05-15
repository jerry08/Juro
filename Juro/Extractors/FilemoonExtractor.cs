using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models.Videos;
using Juro.Utils;
using Juro.Utils.Extensions;

namespace Juro.Extractors;

public class FilemoonExtractor : IVideoExtractor
{
    private readonly Func<HttpClient> _httpClientProvider;

    public string ServerName => "Filemoon";

    public FilemoonExtractor(Func<HttpClient> httpClientProvider)
    {
        _httpClientProvider = httpClientProvider;
    }

    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var http = _httpClientProvider();

        var response = await http.ExecuteAsync(url, cancellationToken);

        var document = Html.Parse(response);

        var scriptNode = document.DocumentNode.Descendants()
            .FirstOrDefault(x => x.Name == "script" && x.InnerText?.Contains("eval") == true);

        var unpacked = JsUnpacker.UnpackAndCombine(scriptNode?.InnerText);

        // Work in progress (subtitles)
        //if (unpacked.Contains("fetch('"))
        //{
        //    var subtitleString = unpacked.SubstringAfter("fetch('")
        //        .Split(new[] { "')." }, StringSplitOptions.None)[0];
        //}

        var masterUrl = unpacked.SubstringAfter("{file:\"")
            .Split(new[] { "\"}" }, StringSplitOptions.None)[0];

        return new List<VideoSource>
        {
            new()
            {
                Format = VideoType.M3u8,
                VideoUrl = masterUrl,
                Resolution = "Multi Quality",
            }
        };
    }
}