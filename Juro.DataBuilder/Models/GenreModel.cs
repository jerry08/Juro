using System.ComponentModel.DataAnnotations;

namespace Juro.DataBuilder.Models;

public class GenreModel
{
    [Key]
    public int GenreId { get; set; }

    public string Name { get; set; } = default!;

    public string? Url { get; set; }

    public GenreModel() { }

    public GenreModel(string name)
    {
        Name = name;
    }

    public GenreModel(string name, string url)
    {
        Name = name;
        Url = url;
    }

    public override string ToString() => $"{Name}";
}
