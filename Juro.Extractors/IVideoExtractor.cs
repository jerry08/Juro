using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core.Models.Videos;

namespace Juro.Extractors;

/// <summary>
/// Interface for basic operations related to a video extractor.
/// </summary>
public interface IVideoExtractor
{
    /// <summary>
    /// Name of the video server.
    /// </summary>
    public string ServerName { get; }

    /// <summary>
    /// Extracts the videos by url.
    /// </summary>
    ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default
    );

    /// <inheritdoc cref="ExtractAsync(string, CancellationToken)" />
    ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    );
}
