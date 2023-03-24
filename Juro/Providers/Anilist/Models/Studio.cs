using System.Collections.Generic;

namespace Juro.Providers.Anilist.Models;

public class Studio
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    public Dictionary<string, List<Media>>? YearMedia { get; set; }
}