﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core;
using Juro.Core.Models;
using Juro.Core.Models.Videos;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;
using Juro.Extractors.Decryptors;

namespace Juro.Extractors;

/// <summary>
/// Extractor for VidCloud.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="VidCloudExtractor"/>.
/// </remarks>
public class VidCloudExtractor(IHttpClientFactory httpClientFactory, bool isAlternative = false)
    : IVideoExtractor
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private readonly string _host = "https://dokicloud.one";
    private readonly string _host2 = "https://rabbitstream.net";
    private readonly bool _isAlternative = isAlternative;

    /// <inheritdoc />
    public string ServerName => "VidCloud";

    /// <summary>
    /// Initializes an instance of <see cref="VidCloudExtractor"/>.
    /// </summary>
    public VidCloudExtractor(Func<HttpClient> httpClientProvider, bool isAlternative = false)
        : this(new HttpClientFactory(httpClientProvider), isAlternative) { }

    /// <summary>
    /// Initializes an instance of <see cref="VidCloudExtractor"/>.
    /// </summary>
    public VidCloudExtractor(bool isAlternative = false)
        : this(Http.ClientProvider, isAlternative) { }

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

        var id = new Stack<string>(url.Split('/')).Pop()?.Split('?')[0];

        headers = new Dictionary<string, string>()
        {
            { "X-Requested-With", "XMLHttpRequest" },
            { "Referer", url },
            { "User-Agent", Http.ChromeUserAgent() },
        };

        var response = await http.ExecuteAsync(
            $"{(_isAlternative ? _host2 : _host)}/ajax/embed-4/getSources?id={id}",
            headers,
            cancellationToken
        );

        var data = JsonNode.Parse(response)!;
        var sourcesJson = data["sources"]!.ToString();

        if (!JsonExtensions.IsValidJson(sourcesJson))
        {
            //var key = await _http.ExecuteAsync("https://raw.githubusercontent.com/consumet/rapidclown/rabbitstream/key.txt", cancellationToken);
            var key = await http.ExecuteAsync(
                "https://raw.githubusercontent.com/enimax-anime/key/e4/key.txt",
                cancellationToken
            );

            var decryptor = new VidCloudDecryptor();
            sourcesJson = decryptor.Decrypt(sourcesJson, key);
        }

        var subtitles = data["tracks"]!
            .AsArray()
            .Where(x => x!["kind"]?.ToString() == "captions")
            .Select(track => new Subtitle()
            {
                Url = track!["file"]!.ToString(),
                Language = track["label"]!.ToString(),
            })
            .ToList();

        var sources = JsonNode.Parse(sourcesJson)!.AsArray()!;

        var list = sources
            .Select(source => new VideoSource()
            {
                VideoUrl = source!["file"]!.ToString(),
                Format = source["file"]!.ToString().Contains(".m3u8")
                    ? VideoType.M3u8
                    : source["type"]!.ToString().ToLower() switch
                    {
                        "hls" => VideoType.Hls,
                        _ => VideoType.Container,
                    },
                Subtitles = subtitles,
            })
            .ToList();

        return list;
    }
}
