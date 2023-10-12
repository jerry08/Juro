namespace Juro.Core.Models.Manga;

public class MangaChapter : IMangaChapter
{
    public string Id { get; set; } = default!;

    public string Title { get; set; } = default!;

    public string? Views { get; set; }

    public string? ReleasedDate { get; set; }
}