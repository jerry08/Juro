using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models.Manga;

namespace Juro.Providers.Manga;

public abstract class MangaParser<TMangaResult, TMangaInfo, TMangaChapterPage>
{
    public HttpClient _http;

    public abstract string Name { get; set; }

    public virtual string BaseUrl => default!;

    public virtual string Logo => default!;

    public MangaParser(HttpClient http)
        => _http = http;

    public abstract Task<List<TMangaResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default!);

    public abstract Task<TMangaInfo> GetMangaInfoAsync(
        string mangaId,
        CancellationToken cancellationToken = default!);

    public abstract Task<List<TMangaChapterPage>> GetChapterPagesAsync(
        string chapterId,
        CancellationToken cancellationToken = default!);
}

public abstract class MangaParser<TMangaResult>
    : MangaParser<TMangaResult, MangaInfo, MangaChapterPage>
{
    public MangaParser(HttpClient http) : base(http) { }
}

public abstract class MangaParser<TMangaResult, TMangaInfo>
    : MangaParser<TMangaResult, TMangaInfo, MangaChapterPage>
{
    public MangaParser(HttpClient http) : base(http) { }
}

public abstract class MangaParser
    : MangaParser<MangaResult, MangaInfo, MangaChapterPage>
{
    public MangaParser(HttpClient http) : base(http) { }
}