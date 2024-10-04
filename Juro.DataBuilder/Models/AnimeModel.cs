using System.ComponentModel.DataAnnotations;
using Juro.Core.Models.Anime;

namespace Juro.DataBuilder.Models;

public class AnimeModel
{
    [Key]
    public int AnimeId { get; set; }

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

    public List<GenreModel> Genres { get; set; } = [];

    public override string ToString() => $"{Title}";
}
