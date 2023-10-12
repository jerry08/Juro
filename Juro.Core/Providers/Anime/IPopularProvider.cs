using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core.Models.Anime;

namespace Juro.Core.Providers;

/// <summary>
/// Interface for basic operations related to popular sources.
/// </summary>
public interface IPopularProvider
{
    /// <summary>
    /// Search for anime.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="List{T}"/> of <see cref="IAnimeInfo"/>s.</returns>
    ValueTask<List<IAnimeInfo>> GetPopularAsync(
        int page = 1,
        CancellationToken cancellationToken = default);
}
