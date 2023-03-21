using System.Collections.Generic;

namespace Juro.Models.Anime;

/// <summary>
/// The Class which contains all the information about an Anime
/// </summary>
public class AnimeInfo
{
    public string Id { get; set; } = default!;

    public AnimeSites Site { get; set; }

    public string Title { get; set; } = default!;

    public string Released { get; set; } = default!;
    public int Episodes { get; set; }
    public string Category { get; set; } = default!;
    public string Link { get; set; } = default!;
    public string Image { get; set; } = default!;

    public string Type { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string Season { get; set; } = default!;
    public int Year { get; set; }
    public float Score { get; set; }
    public string OtherNames { get; set; } = default!;
    public string Summary { get; set; } = default!;
    public List<Genre> Genres { get; set; } = new();
}