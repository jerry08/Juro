using System.Collections.Generic;

namespace Juro.Models.Manga.Mangadex;

public class MangadexResult : MangaResult
{
    public List<string> AltTitles { get; set; } = new();

    public string Description { get; set; } = default!;

    public MediaStatus Status { get; set; }
    
    /// <summary>
    /// Year released
    /// </summary>
    public int ReleaseDate { get; set; }
    
    public string? ContentRating { get; set; }

    public string? LastVolume { get; set; }

    public string? LastChapter { get; set; }
}