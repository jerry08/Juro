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
/// Extractor for StreamSB Pro.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="StreamSBProExtractor"/>.
/// </remarks>
public class StreamSBProExtractor(IHttpClientFactory httpClientFactory) : IVideoExtractor
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private readonly string _alphabet =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    /// <inheritdoc />
    public string ServerName => "StreamSB Pro";

    /// <summary>
    /// Initializes an instance of <see cref="StreamSBProExtractor"/>.
    /// </summary>
    public StreamSBProExtractor(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="StreamSBProExtractor"/>.
    /// </summary>
    public StreamSBProExtractor()
        : this(Http.ClientProvider) { }

    /// <inheritdoc />
    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default
    )
    {
        var http = _httpClientFactory.CreateClient();

        var id = url.FindBetween("/e/", ".html");
        if (string.IsNullOrWhiteSpace(id))
            id = url.Split(new[] { "/e/" }, StringSplitOptions.None)[1];

        var source = await http.ExecuteAsync(
            "https://raw.githubusercontent.com/jerry08/juro-data/main/streamsb.txt",
            cancellationToken
        );

        var jsonLink = $"{source.Trim()}/{Encode(id)}";

        var headers = new Dictionary<string, string>()
        {
            //{ "watchsb", "streamsb" },
            { "watchsb", "sbstream" },
            { "User-Agent", Http.ChromeUserAgent() },
            { "Referer", url },
        };

        var response = await http.ExecuteAsync(jsonLink, headers, cancellationToken);

        var data = JsonNode.Parse(response);
        var masterUrl = data?["stream_data"]?["file"]?.ToString().Trim('"')!;

        return
        [
            new()
            {
                Format = VideoType.M3u8,
                VideoUrl = masterUrl,
                Headers = headers,
                Resolution = "Multi Quality"
            }
        ];
    }

    private string Encode(string id)
    {
        id = $"{MakeId(12)}||{id}||{MakeId(12)}||streamsb";

        var output = "";
        var arr = id.ToArray();

        for (var i = 0; i < arr.Length; i++)
        {
            output += Convert.ToString(Convert.ToInt32(((int)arr[i]).ToString(), 10), 16);
        }

        return output;
    }

    private string MakeId(int length)
    {
        var output = "";

        for (var i = 0; i < length; i++)
        {
            output += _alphabet[(int)Math.Floor(new Random().NextDouble() * _alphabet.Length)];
        }

        return output;
    }
}
