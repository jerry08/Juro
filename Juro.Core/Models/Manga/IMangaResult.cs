using System.Collections.Generic;

namespace Juro.Core.Models.Manga;

public interface IMangaResult
{
    public string Id { get; set; }

    public string? Title { get; set; }

    public string? Image { get; set; }

    public Dictionary<string, string> HeaderForImage { get; set; }
}