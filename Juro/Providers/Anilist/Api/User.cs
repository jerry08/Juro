using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Juro.Providers.Anilist.Api
{
    public class User
    {
        /// <summary>
        /// The id of the user
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// The name of the user
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// The user's avatar images
        /// </summary>
        [JsonPropertyName("avatar")]
        public UserAvatar? Avatar { get; set; }

        /// <summary>
        /// The user's banner images
        /// </summary>
        [JsonPropertyName("bannerImage")]
        public string? BannerImage { get; set; }

        /// <summary>
        /// The user's general options
        /// </summary>
        [JsonPropertyName("options")]
        public UserOptions? Options { get; set; }

        /// <summary>
        /// The user's media list options
        /// </summary>
        [JsonPropertyName("mediaListOptions")]
        public MediaListOptions? MediaListOptions { get; set; }

        /// <summary>
        /// The users favourites
        /// </summary>
        [JsonPropertyName("favourites")]
        public Favourites? Favourites { get; set; }

        /// <summary>
        /// The users anime &#38; manga list statistics
        /// </summary>
        [JsonPropertyName("statistics")]
        public UserStatisticTypes? Statistics { get; set; }
    }

    public class UserOptions
    {
        /// <summary>
        /// Whether the user has enabled viewing of 18+ content
        /// </summary>
        [JsonPropertyName("displayAdultContent")]
        public bool? DisplayAdultContent { get; set; }

        /// <summary>
        /// Whether the user receives notifications when a show they are watching aires
        /// </summary>
        [JsonPropertyName("airingNotifications")]
        public bool? AiringNotifications { get; set; }

        /// <summary>
        /// Profile highlight color (blue, purple, pink, orange, red, green, gray)
        /// </summary>
        [JsonPropertyName("profileColor")]
        public string? ProfileColor { get; set; }
    }

    public class UserAvatar
    {
        /// <summary>
        /// The avatar of user at its largest size
        /// </summary>
        [JsonPropertyName("large")]
        public string? Large { get; set; }

        /// <summary>
        /// The avatar of user at medium size
        /// </summary>
        [JsonPropertyName("medium")]
        public string? Medium { get; set; }
    }

    public class UserStatisticTypes
    {
        [JsonPropertyName("anime")] public UserStatistics? Anime { get; set; }

        [JsonPropertyName("manga")] public UserStatistics? Manga { get; set; }
    }

    public class UserStatistics
    {
        [JsonPropertyName("count")] public int? Count { get; set; }

        [JsonPropertyName("meanScore")] public int? MeanScore { get; set; }

        [JsonPropertyName("standardDeviation")]
        public float? StandardDeviation { get; set; }

        [JsonPropertyName("minutesWatched")] public int? MinutesWatched { get; set; }

        [JsonPropertyName("episodesWatched")] public int? EpisodesWatched { get; set; }

        [JsonPropertyName("chaptersRead")] public int? ChaptersRead { get; set; }

        [JsonPropertyName("volumesRead")] public int? VolumesRead { get; set; }
    }

    public class Favourites
    {
        /// <summary>
        /// Favourite anime
        /// </summary>
        [JsonPropertyName("anime")]
        public MediaConnection? Anime { get; set; }

        /// <summary>
        /// Favourite manga
        /// </summary>
        [JsonPropertyName("manga")]
        public MediaConnection? Manga { get; set; }

        /// <summary>
        /// Favourite characters
        /// </summary>
        [JsonPropertyName("characters")]
        public CharacterConnection? Characters { get; set; }

        /// <summary>
        /// Favourite staff
        /// </summary>
        [JsonPropertyName("staff")]
        public StaffConnection? Staff { get; set; }

        /// <summary>
        /// Favourite studios
        /// </summary>
        [JsonPropertyName("studios")]
        public StudioConnection? Studios { get; set; }
    }

    public class MediaListOptions
    {
        /// <summary>
        /// The default order list rows should be displayed in
        /// </summary>
        [JsonPropertyName("rowOrder")]
        public string? RowOrder { get; set; }

        /// <summary>
        /// The user's anime list options
        /// </summary>
        [JsonPropertyName("animeList")]
        public MediaListTypeOptions? AnimeList { get; set; }

        /// <summary>
        /// The user's manga list options
        /// </summary>
        [JsonPropertyName("mangaList")]
        public MediaListTypeOptions? MangaList { get; set; }
    }

    public class MediaListTypeOptions
    {
        /// <summary>
        /// The order each list should be displayed in
        /// </summary>
        [JsonPropertyName("sectionOrder")]
        public List<string>? SectionOrder { get; set; }

        /// <summary>
        /// If the completed sections of the list should be separated by format
        /// </summary>
        [JsonPropertyName("splitCompletedSectionByFormat")]
        public bool? SplitCompletedSectionByFormat { get; set; }

        /// <summary>
        /// The names of the user's custom lists
        /// </summary>
        [JsonPropertyName("customLists")]
        public List<string>? CustomLists { get; set; }
    }
}