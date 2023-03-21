using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models;
using Juro.Models.Anime;

namespace Juro.Providers.Anime;

public interface IAnimeProvider
{
    public string Name { get; }

    public bool IsDubAvailableSeparately { get; }

    Task<List<AnimeInfo>> SearchAsync(
        string videoUrl,
        CancellationToken cancellationToken = default);

    Task<AnimeInfo> GetAnimeInfoAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<List<Episode>> GetEpisodesAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default);
}