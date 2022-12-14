using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Juro.Models.Videos;

namespace Juro.Extractors;

internal abstract class VideoExtractor
{
    public readonly HttpClient _http;

    public abstract string ServerName { get; }

    public VideoExtractor(HttpClient http)
    {
        _http = http;
    }

    public abstract Task<List<VideoSource>> ExtractAsync(
        string videoUrl,
        CancellationToken cancellationToken = default!);
}