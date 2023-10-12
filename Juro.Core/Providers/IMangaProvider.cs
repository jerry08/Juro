using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core.Models.Manga;

namespace Juro.Core.Providers;

/// <summary>
/// Interface for basic operations related to a manga provider.
/// </summary>
public interface IMangaProvider : ISourceProvider
{
    /// <summary>
    /// Base url of the provider.
    /// </summary>
    public string BaseUrl { get; }

    /// <summary>
    /// Logo of the provider.
    /// </summary>
    public string Logo { get; }

    /// <summary>
    /// Search for manga.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An <see cref="IMangaResult"/> for the provider.</returns>
    ValueTask<List<IMangaResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the manga info by Id.
    /// </summary>
    /// <param name="mangaId">The Id of the manga.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An interface of type <see cref="IMangaInfo"/> for the provider.</returns>
    ValueTask<IMangaInfo> GetMangaInfoAsync(
        string mangaId,
        CancellationToken cancellationToken = default!);

    /// <summary>
    /// Gets chapter pages for manga.
    /// </summary>
    /// <param name="chapterId">The Id of the chapter.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An interface of type <see cref="IMangaChapterPage"/> for the provider.</returns>
    ValueTask<List<IMangaChapterPage>> GetChapterPagesAsync(
        string chapterId,
        CancellationToken cancellationToken = default!);
}