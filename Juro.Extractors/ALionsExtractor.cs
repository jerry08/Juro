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
/// Extractor for ALions.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="ALionsExtractor"/>.
/// </remarks>
public class ALionsExtractor(IHttpClientFactory httpClientFactory) : IVideoExtractor
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    /// <inheritdoc />
    public string ServerName => "ALions / Vidhide";

    /// <summary>
    /// Initializes an instance of <see cref="ALionsExtractor"/>.
    /// </summary>
    public ALionsExtractor(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="ALionsExtractor"/>.
    /// </summary>
    public ALionsExtractor()
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

        var script =
            new Regex("<script type=\"text/javascript\">(eval.+)\n</script>")
                .Match(response)
                .Groups.OfType<Group>()
                .ElementAtOrDefault(1)
                ?.Value
            ?? new Regex("<script type=\'text/javascript\'>(eval.+)\n</script>")
                .Match(response)
                .Groups.OfType<Group>()
                .ElementAtOrDefault(1)
                ?.Value;

        if (string.IsNullOrEmpty(script))
            return [];

        var unpackedScript = JsUnpacker.UnpackAndCombine(script);

        var mediaUrl = new Regex("file:\"([^\"]+)\"\\}")
            .Match(unpackedScript)
            .Groups.OfType<Group>()
            .ElementAtOrDefault(1)
            ?.Value;

        if (string.IsNullOrEmpty(mediaUrl))
            return [];

        return
        [
            new()
            {
                Format = VideoType.M3u8,
                VideoUrl = mediaUrl!,
                Title = ServerName,
            },
        ];
    }
}
