using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core.Models.Videos;

namespace Juro.Core.Providers;

/// <summary>
/// Interface for basic operations related to an anime provider.
/// </summary>
public interface IVideoExtractorProvider
{
    /// <summary>
    /// Gets video sources for episode from server.
    /// </summary>
    /// <param name="server">The server of the episode.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="List{T}"/> of <see cref="VideoSource"/>s.</returns>
    ValueTask<List<VideoSource>> GetVideosAsync(
        VideoServer server,
        CancellationToken cancellationToken = default);
}