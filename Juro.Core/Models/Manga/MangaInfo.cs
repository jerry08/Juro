﻿using System.Collections.Generic;

namespace Juro.Core.Models.Manga;

public class MangaInfo : IMangaInfo
{
    public string Id { get; set; } = default!;

    public string? Title { get; set; }

    public List<string> AltTitles { get; set; } = new();

    public string? Description { get; set; }

    public string? Image { get; set; }

    public Dictionary<string, string> Headers { get; set; } = new();

    public List<string> Genres { get; set; } = new();

    public MediaStatus Status { get; set; }

    public string? Views { get; set; }

    public List<string> Authors { get; set; } = new();

    public List<IMangaChapter> Chapters { get; set; } = new();
}