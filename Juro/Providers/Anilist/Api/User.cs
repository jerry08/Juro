using System.Collections.Generic;
using Newtonsoft.Json;

namespace Juro.Providers.Anilist.Api;

public class User
{
    /// <summary>
    /// The id of the user
    /// </summary>
    [JsonProperty("id")]
    public int Id { get; set; }

    /// <summary>
    /// The name of the user
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The user's avatar images
    /// </summary>
    [JsonProperty("avatar")]
    public UserAvatar? Avatar { get; set; }

    /// <summary>
    /// The user's banner images
    /// </summary>
    [JsonProperty("bannerImage")]
    public string? BannerImage { get; set; }

    /// <summary>
    /// The user's general options
    /// </summary>
    [JsonProperty("options")]
    public UserOptions? Options { get; set; }

    /// <summary>
    /// The user's media list options
    /// </summary>
    [JsonProperty("mediaListOptions")]
    public MediaListOptions? MediaListOptions { get; set; }

    /// <summary>
    /// The users favourites
    /// </summary>
    [JsonProperty("favourites")]
    public Favourites? Favourites { get; set; }

    /// <summary>
    /// The users anime &#38; manga list statistics
    /// </summary>
    [JsonProperty("statistics")]
    public UserStatisticTypes? Statistics { get; set; }
}

public class UserOptions
{
    /// <summary>
    /// Whether the user has enabled viewing of 18+ content
    /// </summary>
    [JsonProperty("displayAdultContent")]
    public bool? DisplayAdultContent { get; set; }

    /// <summary>
    /// Whether the user receives notifications when a show they are watching aires
    /// </summary>
    [JsonProperty("airingNotifications")]
    public bool? AiringNotifications { get; set; }

    /// <summary>
    /// Profile highlight color (blue, purple, pink, orange, red, green, gray)
    /// </summary>
    [JsonProperty("profileColor")]
    public string? ProfileColor { get; set; }
}

public class UserAvatar
{
    /// <summary>
    /// The avatar of user at its largest size
    /// </summary>
    [JsonProperty("large")]
    public string? Large { get; set; }

    /// <summary>
    /// The avatar of user at medium size
    /// </summary>
    [JsonProperty("medium")]
    public string? Medium { get; set; }
}

public class UserStatisticTypes
{
    [JsonProperty("anime")]
    public UserStatistics? Anime { get; set; }

    [JsonProperty("manga")]
    public UserStatistics? Manga { get; set; }
}

public class UserStatistics
{
    [JsonProperty("count")]
    public int? Count { get; set; }

    [JsonProperty("meanScore")]
    public int? MeanScore { get; set; }

    [JsonProperty("standardDeviation")]
    public float? StandardDeviation { get; set; }

    [JsonProperty("minutesWatched")]
    public int? MinutesWatched { get; set; }

    [JsonProperty("episodesWatched")]
    public int? EpisodesWatched { get; set; }

    [JsonProperty("chaptersRead")]
    public int? ChaptersRead { get; set; }

    [JsonProperty("volumesRead")]
    public int? VolumesRead { get; set; }
}

public class Favourites
{
    /// <summary>
    /// Favourite anime
    /// </summary>
    [JsonProperty("anime")]
    public MediaConnection? Anime { get; set; }

    /// <summary>
    /// Favourite manga
    /// </summary>
    [JsonProperty("manga")]
    public MediaConnection? Manga { get; set; }

    /// <summary>
    /// Favourite characters
    /// </summary>
    [JsonProperty("characters")]
    public CharacterConnection? Characters { get; set; }

    /// <summary>
    /// Favourite staff
    /// </summary>
    [JsonProperty("staff")]
    public StaffConnection? Staff { get; set; }

    /// <summary>
    /// Favourite studios
    /// </summary>
    [JsonProperty("studios")]
    public StudioConnection? Studios { get; set; }
}

public class MediaListOptions
{
    /// <summary>
    /// The default order list rows should be displayed in
    /// </summary>
    [JsonProperty("rowOrder")]
    public string? RowOrder { get; set; }

    /// <summary>
    /// The user's anime list options
    /// </summary>
    [JsonProperty("animeList")]
    public MediaListTypeOptions? AnimeList { get; set; }

    /// <summary>
    /// The user's manga list options
    /// </summary>
    [JsonProperty("mangaList")]
    public MediaListTypeOptions? MangaList { get; set; }
}

public class MediaListTypeOptions
{
    /// <summary>
    /// The order each list should be displayed in
    /// </summary>
    [JsonProperty("sectionOrder")]
    public List<string>? SectionOrder { get; set; }

    /// <summary>
    /// If the completed sections of the list should be separated by format
    /// </summary>
    [JsonProperty("splitCompletedSectionByFormat")]
    public bool? SplitCompletedSectionByFormat { get; set; }

    /// <summary>
    /// The names of the user's custom lists
    /// </summary>
    [JsonProperty("customLists")]
    public List<string>? CustomLists { get; set; }
}