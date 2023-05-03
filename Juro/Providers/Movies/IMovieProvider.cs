using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Juro.Providers.Movies;

public interface IMovieProvider
{
    /// <summary>
    /// Name of the provider.
    /// </summary>
    public string Name { get; }

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
    /// <returns>An <see cref="IMangaResult"/> for the provider.</returns>
    Task<List<IMovieInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the manga info by Id.
    /// </summary>
    /// <param name="mangaId">The Id of the manga.</param>
    /// <returns>An interface of type <see cref="IMangaInfo"/> for the provider.</returns>
    Task<IMovieInfo> GetMangaInfoAsync(
        string mangaId,
        CancellationToken cancellationToken = default!);

    /// <summary>
    /// Gets chapter pages for manga.
    /// </summary>
    /// <param name="chapterId">The Id of the chapter.</param>
    /// <returns>An interface of type <see cref="IMangaChapterPage"/> for the provider.</returns>
    Task<List<IMangaChapterPage>> GetChapterPagesAsync(
        string chapterId,
        CancellationToken cancellationToken = default!);
}