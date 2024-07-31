using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core;
using Juro.Core.Models.Videos;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;

namespace Juro.Extractors;

/// <summary>
/// Extractor for YourUpload.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="YourUploadExtractor"/>.
/// </remarks>
public class YourUploadExtractor(IHttpClientFactory httpClientFactory) : IVideoExtractor
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    /// <inheritdoc />
    public string ServerName => "YourUpload";

    /// <summary>
    /// Initializes an instance of <see cref="YourUploadExtractor"/>.
    /// </summary>
    public YourUploadExtractor(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="YourUploadExtractor"/>.
    /// </summary>
    public YourUploadExtractor()
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
    ) => await ExtractAsync(url, quality: null, cancellationToken);

    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        string? quality = null,
        CancellationToken cancellationToken = default
    )
    {
        var http = _httpClientFactory.CreateClient();

        var headers = new Dictionary<string, string>()
        {
            ["Referer"] = "https://www.yourupload.com/"
        };

        var response = await http.ExecuteAsync(url, headers, cancellationToken);

        var document = Html.Parse(response);

        var baseData = document
            .DocumentNode.Descendants()
            .FirstOrDefault(x =>
                x.Name == "script" && x.InnerText?.Contains("jwplayerOptions") == true
            )
            ?.InnerText;

        if (string.IsNullOrEmpty(baseData))
            return [];

        var basicUrl = baseData!.RemoveWhitespaces().SubstringAfter("file:'").SubstringBefore("',");

        return
        [
            new()
            {
                Format = VideoType.Container,
                VideoUrl = basicUrl,
                Resolution = quality,
                Headers = headers,
                Title = $"{quality} - {ServerName}"
            }
        ];
    }
}
