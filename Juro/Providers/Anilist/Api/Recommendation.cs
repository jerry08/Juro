using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Juro.Providers.Anilist.Api
{
    public class Recommendation
    {
        /// <summary>
        /// The id of the recommendation
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Users rating of the recommendation
        /// </summary>
        [JsonPropertyName("rating")]
        public int? Rating { get; set; }

        /// <summary>
        /// The media the recommendation is from
        /// </summary>
        [JsonPropertyName("media")]
        public Media? Media { get; set; }

        /// <summary>
        /// The recommended media
        /// </summary>
        [JsonPropertyName("mediaRecommendation")]
        public Media? MediaRecommendation { get; set; }

        /// <summary>
        /// The user that first created the recommendation
        /// </summary>
        [JsonPropertyName("user")]
        public User? User { get; set; }
    }

    public class RecommendationConnection
    {
        [JsonPropertyName("nodes")] public List<Recommendation>? Nodes { get; set; }
    }
}