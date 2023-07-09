using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Juro.Providers.Anilist.Api
{
    public class Page
    {
        /// <summary>
        /// The pagination information
        /// </summary>
        [JsonPropertyName("pageInfo")]
        public PageInfo? PageInfo { get; set; }

        [JsonPropertyName("users")] public List<User>? Users { get; set; }

        [JsonPropertyName("media")] public List<Media>? Media { get; set; }

        [JsonPropertyName("characters")] public List<Character>? Characters { get; set; }

        [JsonPropertyName("staff")] public List<Staff>? Staff { get; set; }

        [JsonPropertyName("studios")] public List<Studio>? Studio { get; set; }

        [JsonPropertyName("mediaList")] public List<MediaList>? MediaList { get; set; }

        [JsonPropertyName("airingSchedules")] public List<AiringSchedule>? AiringSchedules { get; set; }

        [JsonPropertyName("followers")] public List<User>? Followers { get; set; }

        [JsonPropertyName("following")] public List<User>? Following { get; set; }

        [JsonPropertyName("recommendations")] public List<Recommendation>? Recommendations { get; set; }

        [JsonPropertyName("likes")] public List<User>? Likes { get; set; }
    }

    public class PageInfo
    {
        /// <summary>
        /// The total number of items. Note: This value is not guaranteed to be accurate, do not rely on this for logic
        /// </summary>
        [JsonPropertyName("total")]
        public int? Total { get; set; }

        /// <summary>
        /// The count on a page
        /// </summary>
        [JsonPropertyName("perPage")]
        public int? PerPage { get; set; }

        /// <summary>
        /// The current page
        /// </summary>
        [JsonPropertyName("currentPage")]
        public int? CurrentPage { get; set; }

        /// <summary>
        /// The last page
        /// </summary>
        [JsonPropertyName("lastPage")]
        public int? LastPage { get; set; }

        /// <summary>
        /// If there is another page
        /// </summary>
        [JsonPropertyName("hasNextPage")]
        public bool? HasNextPage { get; set; }
    }
}