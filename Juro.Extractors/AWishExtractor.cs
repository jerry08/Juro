using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core;
using Juro.Core.Models.Videos;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;

namespace Juro.Extractors;

/// <summary>
/// Extractor for AWish.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="AWishExtractor"/>.
/// </remarks>
public class AWishExtractor(IHttpClientFactory httpClientFactory) : IVideoExtractor
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    /// <inheritdoc />
    public string ServerName => "AWish";

    /// <summary>
    /// Initializes an instance of <see cref="AWishExtractor"/>.
    /// </summary>
    public AWishExtractor(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="AWishExtractor"/>.
    /// </summary>
    public AWishExtractor()
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
        var http = _httpClientFactory.CreateClient();

        var response = await http.ExecuteAsync(url, cancellationToken);

        var document = Html.Parse(response);

        var script = document
            .DocumentNode.Descendants()
            .FirstOrDefault(x => x.Name == "script" && x.InnerText?.Contains("m3u8") == true)
            ?.InnerText;

        // Sometimes the script body is packed, sometimes it isn't
        var scriptBody = JsUnpacker.IsPacked(script) ? JsUnpacker.UnpackAndCombine(script) : script;

        if (string.IsNullOrEmpty(scriptBody))
            return [];

        //var mediaUrl = new Regex("file:\"([^\"]+)\"\\}").Match(scriptBody)
        var mediaUrl = new Regex("file:\"([^\"]+)\"")
            .Match(scriptBody)
            .Groups.OfType<Group>()
            .ToList()[1]
            .Value;

        return
        [
            new()
            {
                Format = VideoType.M3u8,
                VideoUrl = mediaUrl,
                Title = ServerName
            }
        ];
    }
}
