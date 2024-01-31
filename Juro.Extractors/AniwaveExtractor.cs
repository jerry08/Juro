using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core;
using Juro.Core.Models.Videos;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;

namespace Juro.Extractors;

/// <summary>
/// Extractor for Aniwave.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="AniwaveExtractor"/>.
/// </remarks>
public class AniwaveExtractor(IHttpClientFactory httpClientFactory, string serverName)
    : IVideoExtractor
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    /// <summary>
    /// Name of the video server for Aniwave. It can either be "Mcloud" or "Vizcloud"
    /// </summary>
    public string ServerName { get; } = serverName;

    /// <summary>
    /// Initializes an instance of <see cref="AniwaveExtractor"/>.
    /// </summary>
    public AniwaveExtractor(Func<HttpClient> httpClientProvider, string serverName)
        : this(new HttpClientFactory(httpClientProvider), serverName) { }

    /// <summary>
    /// Initializes an instance of <see cref="AniwaveExtractor"/>.
    /// </summary>
    public AniwaveExtractor(string serverName)
        : this(Http.ClientProvider, serverName) { }

    /// <inheritdoc />
    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default
    )
    {
        var http = _httpClientFactory.CreateClient();

        var list = new List<VideoSource>();

        var isMcloud = ServerName.Equals("MyCloud", StringComparison.OrdinalIgnoreCase);
        var server = isMcloud ? "Mcloud" : "Vizcloud";
        var vidId = new Stack<string>(url.Split('/')).Pop()?.Split('?').FirstOrDefault();
        var url2 = $"https://9anime.eltik.net/raw{server}?query={vidId}&apikey=chayce";

        var response = await http.ExecuteAsync(url2, cancellationToken);
        var apiUrl = JsonNode.Parse(response)?["rawURL"]?.ToString();
        if (string.IsNullOrWhiteSpace(apiUrl))
            return list;

        var referer = isMcloud ? "https://mcloud.to/" : "https://9anime.to/";

        response = await http.ExecuteAsync(
            apiUrl!,
            new() { ["Referer"] = referer },
            cancellationToken
        );

        var data = JsonNode.Parse(response)!["data"]!;

        var file = data["media"]!["sources"]![0]!["file"]!.ToString();

        list.Add(
            new()
            {
                VideoUrl = file,
                Headers = new() { ["Referer"] = referer },
                Format = VideoType.M3u8,
                Resolution = "Multi Quality",
            }
        );

        return list;
    }
}
