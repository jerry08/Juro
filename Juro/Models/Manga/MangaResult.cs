using System.Collections.Generic;

namespace Juro.Models.Manga;

public class MangaResult
{
    public string Id { get; set; } = default!;

    public string? Title { get; set; }

    public string? Image { get; set; }

    public Dictionary<string, string> HeaderForImage { get; set; } = new();
}