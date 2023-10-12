using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core;
using Juro.Core.Models.Videos;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;

namespace Juro.Extractors;

/// <summary>
/// Extractor for Filemoon.
/// </summary>
public class FilemoonExtractor : IVideoExtractor
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <inheritdoc />
    public string ServerName => "Filemoon";

    /// <summary>
    /// Initializes an instance of <see cref="FilemoonExtractor"/>.
    /// </summary>
    public FilemoonExtractor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Initializes an instance of <see cref="FilemoonExtractor"/>.
    /// </summary>
    public FilemoonExtractor(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider))
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="FilemoonExtractor"/>.
    /// </summary>
    public FilemoonExtractor() : this(Http.ClientProvider)
    {
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var http = _httpClientFactory.CreateClient();

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