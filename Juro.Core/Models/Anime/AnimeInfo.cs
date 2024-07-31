using System.Collections.Generic;

namespace Juro.Core.Models.Anime;

/// <summary>
/// The Class which contains all the information about an Anime
/// </summary>
public class AnimeInfo : IAnimeInfo
{
    public string Id { get; set; } = default!;

    public AnimeSites Site { get; set; }

    public string Title { get; set; } = default!;

    public string? Released { get; set; }

    public string? Category { get; set; }

    public string? Link { get; set; }

    public string? Image { get; set; }

    public string? Type { get; set; }

    public string? Status { get; set; }

    public string? OtherNames { get; set; }

    public string? Summary { get; set; }

    public List<Genre> Genres { get; set; } = [];

    public override string ToString() => $"{Title}";
}
