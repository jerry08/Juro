namespace Juro.Models.Anime;

/// <summary>
/// The Class which contains all the information about a Genre
/// </summary>
public class Genre
{
    public string Name { get; set; } = default!;

    public string Link { get; set; } = default!;

    public Genre()
    {
    }

    public Genre(string name)
    {
        Name = name;
    }

    public Genre(string name, string link)
    {
        Name = name;
        Link = link;
    }
}