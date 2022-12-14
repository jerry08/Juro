using System.Collections.Generic;

namespace Juro.Models.Manga.Mangadex;

public class MangadexInfo : MangaInfo<MangadexChapter>
{
    /// <summary>
    /// Year released
    /// </summary>
    public int ReleaseDate { get; set; }

    public List<string> Themes { get; set; } = new();
}