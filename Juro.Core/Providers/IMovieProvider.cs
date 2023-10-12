using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core.Models.Movie;
using Juro.Core.Models.Videos;

namespace Juro.Core.Providers;

/// <summary>
/// Interface for basic operations related to a movie provider.
/// </summary>
public interface IMovieProvider : ISourceProvider, IVideoExtractorProvider
{
    /// <summary>
    /// Search for movies.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="List{T}"/> of <see cref="MovieResult"/>s.</returns>
    ValueTask<List<MovieResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the movie info by Id.
    /// </summary>
    /// <param name="mediaId">The movie Id.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An instance of <see cref="MovieInfo"/> for the provider.</returns>
    ValueTask<MovieInfo> GetMediaInfoAsync(
        string mediaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets episodes for movie.
    /// </summary>
    /// <param name="episodeId">EpisodeId takes episode link or movie id.</param>
    /// <param name="mediaId">MediaId takes movie link or id (found on movie info object).</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="List{T}"/> of <see cref="VideoServer"/>s.</returns>
    ValueTask<List<VideoServer>> GetEpisodeServersAsync(
        string episodeId,
        string mediaId,
        CancellationToken cancellationToken = default);
}