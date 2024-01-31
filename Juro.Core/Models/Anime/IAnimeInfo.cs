using System.Collections.Generic;

namespace Juro.Core.Models.Anime;

/// <summary>
/// The Class which contains all the information about an Anime
/// </summary>
public interface IAnimeInfo
{
    public string Id { get; set; }

    public AnimeSites Site { get; set; }

    public string Title { get; set; }

    public string? Released { get; set; }

    public string? Category { get; set; }

    public string? Link { get; set; }

    public string? Image { get; set; }

    public string? Type { get; set; }

    public string? Status { get; set; }

    public string? OtherNames { get; set; }

    public string? Summary { get; set; }

    public List<Genre> Genres { get; set; }
}
