using System.Collections.Generic;

namespace Juro.Core.Models.Manga;

public class MangaChapterPage : IMangaChapterPage
{
    public string Image { get; set; } = default!;

    public int Page { get; set; }

    public string? Title { get; set; }

    public Dictionary<string, string> Headers { get; set; } = [];

    public override string ToString() => $"{Title}";
}
