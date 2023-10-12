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
/// Extractor for Mp4upload.
/// </summary>
public class Mp4uploadExtractor : IVideoExtractor
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <inheritdoc />
    public string ServerName => "Mp4upload";

    /// <summary>
    /// Initializes an instance of <see cref="Mp4uploadExtractor"/>.
    /// </summary>
    public Mp4uploadExtractor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Initializes an instance of <see cref="Mp4uploadExtractor"/>.
    /// </summary>
    public Mp4uploadExtractor(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider))
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="Mp4uploadExtractor"/>.
    /// </summary>
    public Mp4uploadExtractor() : this(Http.ClientProvider)
    {
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var http = _httpClientFactory.CreateClient();

        var headers = new Dictionary<string, string>()
        {
            ["Referer"] = "https://mp4upload.com/"
        };

        var response = await http.ExecuteAsync(url, headers, cancellationToken);

        var document = Html.Parse(response);

        var link = document.DocumentNode.Descendants()
            .Where(x => x.Name == "script")
            .FirstOrDefault(x => x.InnerText.Contains("src: "))
            ?.InnerText.SubstringAfter("src: \"").SubstringBefore("\"");
        if (!string.IsNullOrWhiteSpace(link))
        {
            var host = link!.SubstringAfter("https://").SubstringBefore("/");
            headers.Add("host", host);

            return new List<VideoSource>
            {
                new()
                {
                    Format = VideoType.Container,
                    VideoUrl = link!,
                    Resolution = "Default Quality",
                    Headers = headers
                }
            };
        }

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