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
public class VidStreamExtractor : IVideoExtractor
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <inheritdoc />
    public string ServerName => "VidStream";

    /// <summary>
    /// Initializes an instance of <see cref="VidStreamExtractor"/>.
    /// </summary>
    public VidStreamExtractor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Initializes an instance of <see cref="VidStreamExtractor"/>.
    /// </summary>
    public VidStreamExtractor(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider))
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="VidStreamExtractor"/>.
    /// </summary>
    public VidStreamExtractor() : this(Http.ClientProvider)
    {
    }

    /// <inheritdoc />
    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var http = _httpClientFactory.CreateClient();

        var response = await http.ExecuteAsync(url, cancellationToken);

        if (url.Contains("srcd"))
        {
            var link = response.FindBetween("\"file\": '", "',");

            return new List<VideoSource>
            {
                new()
                {
                    Format = VideoType.M3u8,
                    VideoUrl = link,
                    Title = ServerName
                }
            };
        }

        var document = Html.Parse(response);

        var mediaUrl = document.DocumentNode.SelectSingleNode(".//iframe")?.Attributes["src"]?.Value;
        if (string.IsNullOrWhiteSpace(mediaUrl))
            return new();

        if (mediaUrl!.Contains("filemoon"))
            return await new FilemoonExtractor(_httpClientFactory).ExtractAsync(mediaUrl, cancellationToken);

        return await new GogoCDNExtractor(_httpClientFactory).ExtractAsync(mediaUrl, cancellationToken);
    }
}