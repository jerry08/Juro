namespace Juro.Core.Models.Anime;

/// <summary>
/// The Class which contains all the information about a Genre.
/// </summary>
public class Genre
{
    public string Name { get; set; } = default!;

    public string? Url { get; set; }

    public Genre()
    {
    }

    public Genre(string name)
    {
        Name = name;
    }

    public Genre(string name, string url)
    {
        Name = name;
        Url = url;
    }
}