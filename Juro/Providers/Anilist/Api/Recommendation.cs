using System.Collections.Generic;
using Newtonsoft.Json;

namespace Juro.Providers.Anilist.Api;

public class Recommendation
{
    /// <summary>
    /// The id of the recommendation
    /// </summary>
    [JsonProperty("id")]
    public int Id { get; set; }

    /// <summary>
    /// Users rating of the recommendation
    /// </summary>
    [JsonProperty("rating")]
    public int? Rating { get; set; }

    /// <summary>
    /// The media the recommendation is from
    /// </summary>
    [JsonProperty("media")]
    public Media? Media { get; set; }

    /// <summary>
    /// The recommended media
    /// </summary>
    [JsonProperty("mediaRecommendation")]
    public Media? MediaRecommendation { get; set; }

    /// <summary>
    /// The user that first created the recommendation
    /// </summary>
    [JsonProperty("user")]
    public User? User { get; set; }
}

public class RecommendationConnection
{
    [JsonProperty("nodes")]
    public List<Recommendation>? Nodes { get; set; }
}