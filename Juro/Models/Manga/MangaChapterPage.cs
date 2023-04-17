using System.Collections.Generic;

namespace Juro.Models.Manga;

public class MangaChapterPage : IMangaChapterPage
{
    public string Image { get; set; } = default!;

    public int Page { get; set; }

    public string? Title { get; set; }

    public Dictionary<string, string> HeaderForImage { get; set; } = new();
}