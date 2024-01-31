namespace Juro.Core.Models.Manga;

public class MangaChapter : IMangaChapter
{
    public string Id { get; set; } = default!;

    public float Number { get; set; }

    public string? Title { get; set; }

    public string? Views { get; set; }

    public string? ReleasedDate { get; set; }

    public override string ToString() => $"({Number}) {Title}";
}
