using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core.Models.Anime;

namespace Juro.Core.Providers;

/// <summary>
/// Interface for basic operations related to recently added sources.
/// </summary>
public interface IRecentlyAddedProvider
{
    /// <summary>
    /// Search for anime.
    /// </summary>
    /// <returns>A <see cref="List{T}"/> of <see cref="IAnimeInfo"/>s.</returns>
    ValueTask<List<IAnimeInfo>> GetRecentlyAddedAsync(
        int page = 1,
        CancellationToken cancellationToken = default
    );
}
