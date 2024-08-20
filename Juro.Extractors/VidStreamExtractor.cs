using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core;
using Juro.Core.Models.Videos;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;

namespace Juro.Extractors;

/// <summary>
/// Extractor for VidStream.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="VidStreamExtractor"/>.
/// </remarks>
public class VidStreamExtractor(IHttpClientFactory httpClientFactory) : IVideoExtractor
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    /// <inheritdoc />
    public string ServerName => "VidStream";

    /// <summary>
    /// Initializes an instance of <see cref="VidStreamExtractor"/>.
    /// </summary>
    public VidStreamExtractor(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="VidStreamExtractor"/>.
    /// </summary>
    public VidStreamExtractor()
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

        if (url.Contains("srcd"))
        {
            var link = response.FindBetween("\"file\": '", "',");

            return
            [
                new()
                {
                    Format = VideoType.M3u8,
                    VideoUrl = link,
                    Title = ServerName,
                },
            ];
        }

        var document = Html.Parse(response);

        var mediaUrl = document
            .DocumentNode.SelectSingleNode(".//iframe")
            ?.Attributes["src"]
            ?.Value;
        if (string.IsNullOrWhiteSpace(mediaUrl))
            return [];

        if (mediaUrl!.Contains("filemoon"))
            return await new FilemoonExtractor(_httpClientFactory).ExtractAsync(
                mediaUrl,
                cancellationToken
            );

        return await new GogoCDNExtractor(_httpClientFactory).ExtractAsync(
            mediaUrl,
            cancellationToken
        );
    }
}
