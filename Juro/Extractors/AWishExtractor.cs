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

/// <summary>
/// Extractor for AWish.
/// </summary>
public class AWishExtractor : IVideoExtractor
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <inheritdoc />
    public string ServerName => "AWish";

    /// <summary>
    /// Initializes an instance of <see cref="AWishExtractor"/>.
    /// </summary>
    public AWishExtractor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Initializes an instance of <see cref="AWishExtractor"/>.
    /// </summary>
    public AWishExtractor(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider))
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="AWishExtractor"/>.
    /// </summary>
    public AWishExtractor() : this(Http.ClientProvider)
    {
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var http = _httpClientFactory.CreateClient();

        var response = await http.ExecuteAsync(url, cancellationToken);

        var mediaUrl = new Regex("file:\"([^\"]+)\"\\}").Match(response)
            .Groups.OfType<Group>()
            .ToList()[1]
            .Value;

        return new List<VideoSource>
        {
            new()
            {
                Format = VideoType.M3u8,
                VideoUrl = mediaUrl,
                Title = ServerName
            }
        };
    }
}