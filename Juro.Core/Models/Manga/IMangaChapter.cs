namespace Juro.Core.Models.Manga;

public interface IMangaChapter
{
    public string Id { get; set; }

    public string Title { get; set; }

    public string? Views { get; set; }

    public string? ReleasedDate { get; set; }
}
