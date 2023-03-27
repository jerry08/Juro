using System.Collections.Generic;
using Newtonsoft.Json;

namespace Juro.Providers.Anilist.Api;

public class Page
{
    /// <summary>
    /// The pagination information
    /// </summary>
    [JsonProperty("pageInfo")]
    public PageInfo? PageInfo { get; set; }

    [JsonProperty("users")]
    public List<User>? Users { get; set; }

    [JsonProperty("media")]
    public List<Media>? Media { get; set; }

    [JsonProperty("characters")]
    public List<Character>? Characters { get; set; }

    [JsonProperty("staff")]
    public List<Staff>? Staff { get; set; }

    [JsonProperty("studios")]
    public List<Studio>? Studio { get; set; }

    [JsonProperty("mediaList")]
    public List<MediaList>? MediaList { get; set; }

    [JsonProperty("airingSchedules")]
    public List<AiringSchedule>? AiringSchedules { get; set; }

    [JsonProperty("followers")]
    public List<User>? Followers { get; set; }

    [JsonProperty("following")]
    public List<User>? Following { get; set; }

    [JsonProperty("recommendations")]
    public List<Recommendation>? Recommendations { get; set; }

    [JsonProperty("likes")]
    public List<User>? Likes { get; set; }
}

public class PageInfo
{
    /// <summary>
    /// The total number of items. Note: This value is not guaranteed to be accurate, do not rely on this for logic
    /// </summary>
    [JsonProperty("total")]
    public int? Total { get; set; }

    /// <summary>
    /// The count on a page
    /// </summary>
    [JsonProperty("perPage")]
    public int? PerPage { get; set; }

    /// <summary>
    /// The current page
    /// </summary>
    [JsonProperty("currentPage")]
    public int? CurrentPage { get; set; }

    /// <summary>
    /// The last page
    /// </summary>
    [JsonProperty("lastPage")]
    public int? LastPage { get; set; }

    /// <summary>
    /// If there is another page
    /// </summary>
    [JsonProperty("hasNextPage")]
    public bool? HasNextPage { get; set; }
}