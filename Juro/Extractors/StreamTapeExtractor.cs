using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models.Videos;
using Juro.Utils;
using Juro.Utils.Extensions;

namespace Juro.Extractors;

/// <summary>
/// Extractor for StreamTape.
/// </summary>
public class StreamTapeExtractor : IVideoExtractor
{
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly Regex _linkRegex = new(@"'robotlink'\)\.innerHTML = '(.+?)'\+ \('(.+?)'\)");

    /// <inheritdoc />
    public string ServerName => "StreamTape";

    /// <summary>
    /// Initializes an instance of <see cref="StreamTapeExtractor"/>.
    /// </summary>
    public StreamTapeExtractor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Initializes an instance of <see cref="StreamTapeExtractor"/>.
    /// </summary>
    public StreamTapeExtractor(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider))
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="StreamTapeExtractor"/>.
    /// </summary>
    public StreamTapeExtractor() : this(Http.ClientProvider)
    {
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var http = _httpClientFactory.CreateClient();

        var id = url.Split(new[] { "/e/" }, StringSplitOptions.None)[1];

        var response = await http.ExecuteAsync(
            url.Replace("tape.com", "adblocker.xyz"),
            cancellationToken
        );

        var reg = _linkRegex.Match(response);

        var vidUrl = $"https:{reg.Groups[1]!.Value + reg.Groups[2]!.Value.Substring(3)}";

        return new List<VideoSource>
        {
            new()
            {
                Format = VideoType.M3u8,
                VideoUrl = vidUrl,
                Resolution = "Multi Quality",
            }
        };
    }
}