﻿using System;
using System.Collections.Generic;
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
/// Extractor for OkRu.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="OkRuExtractor"/>.
/// </remarks>
public class OkRuExtractor(IHttpClientFactory httpClientFactory) : IVideoExtractor
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    /// <inheritdoc />
    public string ServerName => "OkRu";

    /// <summary>
    /// Initializes an instance of <see cref="OkRuExtractor"/>.
    /// </summary>
    public OkRuExtractor(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="OkRuExtractor"/>.
    /// </summary>
    public OkRuExtractor()
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

        var mediaUrl = new Regex("https://vd\\d+\\.mycdn\\.me/e[^\\\\]+").Match(response);

        return
        [
            new()
            {
                Format = VideoType.M3u8,
                VideoUrl = mediaUrl.Value,
                Title = ServerName,
            },
            new()
            {
                Format = VideoType.Dash,
                VideoUrl = mediaUrl.NextMatch().Value,
                Title = ServerName,
            },
        ];
    }
}
