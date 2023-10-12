namespace Juro.Core.Models.Movie;

public class MovieResult
{
    public string Id { get; set; } = default!;

    public string? Title { get; set; }

    public string? Url { get; set; }

    public string? Image { get; set; }

    public string? ReleasedDate { get; set; }

    public TvType Type { get; set; }
}