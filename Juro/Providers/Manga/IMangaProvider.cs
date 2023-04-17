using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models.Manga;

namespace Juro.Providers.Manga;

public interface IMangaProvider
{
    public string Name { get; }

    public string BaseUrl { get; }

    public string Logo { get; }

    Task<List<IMangaResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default);

    Task<IMangaInfo> GetMangaInfoAsync(
        string mangaId,
        CancellationToken cancellationToken = default!);

    Task<List<IMangaChapterPage>> GetChapterPagesAsync(
        string chapterId,
        CancellationToken cancellationToken = default!);
}