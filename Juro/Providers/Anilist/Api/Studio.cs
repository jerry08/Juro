using System.Collections.Generic;
using Newtonsoft.Json;

namespace Juro.Providers.Anilist.Api;

public class Studio
{
    /// <summary>
    /// The id of the studio
    /// </summary>
    [JsonProperty("id")]
    public int Id { get; set; }

    /// <summary>
    /// The name of the studio
    /// Originally non-nullable, needs to be nullable due to it not being always queried
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// If the studio is an animation studio or a different kind of company
    /// </summary>
    [JsonProperty("isAnimationStudio")]
    public bool? IsAnimationStudio { get; set; }

    /// <summary>
    /// The media the studio has worked on
    /// </summary>
    [JsonProperty("media")]
    public MediaConnection? Media { get; set; }

    /// <summary>
    /// The url for the studio page on the AniList website
    /// </summary>
    [JsonProperty("siteUrl")]
    public string? SiteUrl { get; set; }

    /// <summary>
    /// If the studio is marked as favourite by the currently authenticated user
    /// </summary>
    [JsonProperty("isFavourite")]
    public bool? IsFavourite { get; set; }

    /// <summary>
    /// The amount of user's who have favourited the studio
    /// </summary>
    [JsonProperty("favourites")]
    public int? Favourites { get; set; }
}

public class StudioConnection
{
    [JsonProperty("nodes")]
    public List<Studio>? Nodes { get; set; }
}