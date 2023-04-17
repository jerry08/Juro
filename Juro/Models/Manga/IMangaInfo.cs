using System.Collections.Generic;

namespace Juro.Models.Manga;

public interface IMangaInfo
{
    public string Id { get; set; }

    public string? Title { get; set; }

    public List<string> AltTitles { get; set; }

    public string? Description { get; set; }

    public string? Image { get; set; }

    public Dictionary<string, string> HeaderForImage { get; set; }

    public List<string> Genres { get; set; }

    public MediaStatus Status { get; set; }

    public string? Views { get; set; }

    public List<string> Authors { get; set; }

    public List<IMangaChapter> Chapters { get; set; }
}