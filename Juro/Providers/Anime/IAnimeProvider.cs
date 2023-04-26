using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models;
using Juro.Models.Anime;
using Juro.Models.Videos;

namespace Juro.Providers.Anime;

/// <summary>
/// Interface for basic operations related to an anime provider.
/// </summary>
public interface IAnimeProvider
{
    /// <summary>
    /// Name of the provider.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// If dub is available separately for the provider.
    /// </summary>
    public bool IsDubAvailableSeparately { get; }

    /// <summary>
    /// Search for anime.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <returns>An instance of <see cref="AnimeInfo"/> for the provider.</returns>
    Task<List<AnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the anime info by Id.
    /// </summary>
    /// <param name="id">The anime Id.</param>
    /// <returns>An instance of <see cref="AnimeInfo"/> for the provider.</returns>
    Task<AnimeInfo> GetAnimeInfoAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets episodes for anime.
    /// </summary>
    /// <param name="id">The anime Id.</param>
    /// <returns>A <see cref="List{T}"/> of <see cref="Episode"/>s.</returns>
    Task<List<Episode>> GetEpisodesAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets video servers for episode.
    /// </summary>
    /// <param name="episodeId">The episode Id.</param>
    /// <returns>A <see cref="List{T}"/> of <see cref="VideoServer"/>s.</returns>
    Task<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets video sources for episode from server.
    /// </summary>
    /// <param name="server">The server of the episode.</param>
    /// <returns>A <see cref="List{T}"/> of <see cref="VideoSource"/>s.</returns>
    Task<List<VideoSource>> GetVideosAsync(
        VideoServer server,
        CancellationToken cancellationToken = default);
}