using System.Collections.Generic;

namespace Juro.Core.Models.Movie;

public class MovieInfo
{
    public string Id { get; set; } = default!;

    public string? Title { get; set; }

    public string? Image { get; set; }

    public string? Description { get; set; }

    public string? ReleasedDate { get; set; }

    public TvType Type { get; set; }

    public List<string> Genres { get; set; } = [];

    public List<string> Tags { get; set; } = [];

    public List<string> Casts { get; set; } = [];

    public string? Production { get; set; }

    public string? Country { get; set; }

    public string? Duration { get; set; }

    public string? Rating { get; set; }

    public List<Episode> Episodes { get; set; } = [];
}
