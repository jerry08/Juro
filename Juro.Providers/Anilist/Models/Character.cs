using System.Collections.Generic;
using Juro.Providers.Anilist.Api;

namespace Juro.Providers.Anilist.Models;

public class Character
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Image { get; set; }
    public string? Banner { get; set; }
    public string? Role { get; set; }

    public string? Description { get; set; }
    public string? Age { get; set; }
    public string? Gender { get; set; }
    public FuzzyDate? DateOfBirth { get; set; }
    public List<Media>? Roles { get; set; }
}