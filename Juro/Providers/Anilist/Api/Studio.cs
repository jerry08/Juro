using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Juro.Providers.Anilist.Api
{
    public class Studio
    {
        /// <summary>
        /// The id of the studio
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// The name of the studio
        /// Originally non-nullable, needs to be nullable due to it not being always queried
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// If the studio is an animation studio or a different kind of company
        /// </summary>
        [JsonPropertyName("isAnimationStudio")]
        public bool? IsAnimationStudio { get; set; }

        /// <summary>
        /// The media the studio has worked on
        /// </summary>
        [JsonPropertyName("media")]
        public MediaConnection? Media { get; set; }

        /// <summary>
        /// The url for the studio page on the AniList website
        /// </summary>
        [JsonPropertyName("siteUrl")]
        public string? SiteUrl { get; set; }

        /// <summary>
        /// If the studio is marked as favourite by the currently authenticated user
        /// </summary>
        [JsonPropertyName("isFavourite")]
        public bool? IsFavourite { get; set; }

        /// <summary>
        /// The amount of user's who have favourited the studio
        /// </summary>
        [JsonPropertyName("favourites")]
        public int? Favourites { get; set; }
    }

    public class StudioConnection
    {
        [JsonPropertyName("nodes")] public List<Studio>? Nodes { get; set; }
    }
}