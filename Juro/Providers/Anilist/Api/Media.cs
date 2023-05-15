using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Juro.Providers.Anilist.Api;

public class Media
{
    /// <summary>
    /// The id of the media
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// The mal id of the media
    /// </summary>
    [JsonPropertyName("idMal")]
    public int? IdMal { get; set; }

    /// <summary>
    /// The official titles of the media in various languages
    /// </summary>
    [JsonPropertyName("title")]
    public MediaTitle? Title { get; set; }

    /// <summary>
    /// The type of the media; anime or manga
    /// </summary>
    [JsonPropertyName("type")]
    public MediaType? Type { get; set; }

    /// <summary>
    /// The format the media was released in
    /// </summary>
    [JsonPropertyName("format")]
    public MediaFormat? Format { get; set; }

    /// <summary>
    /// The current releasing status of the media
    /// </summary>
    [JsonPropertyName("status")]
    public MediaStatus? Status { get; set; }

    /// <summary>
    /// Short description of the media's story and characters
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The first official release date of the media
    /// </summary>
    [JsonPropertyName("startDate")]
    public FuzzyDate? StartDate { get; set; }

    /// <summary>
    /// The last official release date of the media
    /// </summary>
    [JsonPropertyName("endDate")]
    public FuzzyDate? EndDate { get; set; }

    /// <summary>
    /// The season the media was initially released in
    /// </summary>
    [JsonPropertyName("season")]
    public MediaSeason? Season { get; set; }

    /// <summary>
    /// The season year the media was initially released in
    /// </summary>
    [JsonPropertyName("seasonYear")]
    public int? SeasonYear { get; set; }

    /// <summary>
    /// The year &#38; season the media was initially released in
    /// </summary>
    [JsonPropertyName("seasonInt")]
    public int? SeasonInt { get; set; }

    /// <summary>
    /// The amount of episodes the anime has when complete
    /// </summary>
    [JsonPropertyName("episodes")]
    public int? Episodes { get; set; }

    /// <summary>
    /// The general length of each anime episode in minutes
    /// </summary>
    [JsonPropertyName("duration")]
    public int? Duration { get; set; }

    /// <summary>
    /// The amount of chapters the manga has when complete
    /// </summary>
    [JsonPropertyName("chapters")]
    public int? Chapters { get; set; }

    /// <summary>
    /// The amount of volumes the manga has when complete
    /// </summary>
    [JsonPropertyName("volumes")]
    public int? Volumes { get; set; }

    /// <summary>
    /// Where the media was created. (ISO 3166-1 alpha-2)
    /// Originally a "CountryCode"
    /// </summary>
    [JsonPropertyName("countryOfOrigin")]
    public string? CountryOfOrigin { get; set; }

    /// <summary>
    /// Source type the media was adapted from.
    /// </summary>
    [JsonPropertyName("source")]
    public MediaSource? Source { get; set; }

    /// <summary>
    /// Official Twitter hashtags for the media
    /// </summary>
    [JsonPropertyName("hashtag")]
    public string? Hashtag { get; set; }

    /// <summary>
    /// Media trailer or advertisement
    /// </summary>
    [JsonPropertyName("trailer")]
    public MediaTrailer? Trailer { get; set; }

    /// <summary>
    /// When the media's data was last updated
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public int? UpdatedAt { get; set; }

    /// <summary>
    /// The cover images of the media
    /// </summary>
    [JsonPropertyName("coverImage")]
    public MediaCoverImage? CoverImage { get; set; }

    /// <summary>
    /// The banner image of the media
    /// </summary>
    [JsonPropertyName("bannerImage")]
    public string? BannerImage { get; set; }

    /// <summary>
    /// The genres of the media
    /// </summary>
    [JsonPropertyName("genres")]
    public List<string>? Genres { get; set; }

    /// <summary>
    /// Alternative titles of the media
    /// </summary>
    [JsonPropertyName("synonyms")]
    public List<string>? Synonyms { get; set; }

    /// <summary>
    /// A weighted average score of all the user's scores of the media
    /// </summary>
    [JsonPropertyName("averageScore")]
    public int? AverageScore { get; set; }

    /// <summary>
    /// Mean score of all the user's scores of the media
    /// </summary>
    [JsonPropertyName("meanScore")]
    public int? MeanScore { get; set; }

    /// <summary>
    /// The number of users with the media on their list
    /// </summary>
    [JsonPropertyName("popularity")]
    public int? Popularity { get; set; }

    /// <summary>
    /// Locked media may not be added to lists our favorited. This may be due to the entry pending for deletion or other reasons.
    /// </summary>
    [JsonPropertyName("isLocked")]
    public bool? IsLocked { get; set; }

    /// <summary>
    /// The amount of related activity in the past hour
    /// </summary>
    [JsonPropertyName("trending")]
    public int? Trending { get; set; }

    /// <summary>
    /// The amount of user's who have favourited the media
    /// </summary>
    [JsonPropertyName("favourites")]
    public int? Favourites { get; set; }

    /// <summary>
    /// List of tags that describes elements and themes of the media
    /// </summary>
    [JsonPropertyName("tags")]
    public List<MediaTag>? Tags { get; set; }

    /// <summary>
    /// Other media in the same or connecting franchise
    /// </summary>
    [JsonPropertyName("relations")]
    public MediaConnection? Relations { get; set; }

    /// <summary>
    /// The characters in the media
    /// </summary>
    [JsonPropertyName("characters")]
    public CharacterConnection? Characters { get; set; }

    /// <summary>
    /// The companies who produced the media
    /// </summary>
    [JsonPropertyName("studios")]
    public StudioConnection? Studios { get; set; }

    /// <summary>
    /// If the media is marked as favourite by the current authenticated user
    /// </summary>
    [JsonPropertyName("isFavourite")]
    public bool? IsFavourite { get; set; }

    /// <summary>
    /// If the media is blocked from being added to favourites
    /// </summary>
    [JsonPropertyName("isFavouriteBlocked")]
    public bool? IsFavouriteBlocked { get; set; }

    /// <summary>
    /// If the media is intended only for 18+ adult audiences
    /// </summary>
    [JsonPropertyName("isAdult")]
    public bool? IsAdult { get; set; }

    /// <summary>
    /// The media's next episode airing schedule
    /// </summary>
    [JsonPropertyName("nextAiringEpisode")]
    public AiringSchedule? NextAiringEpisode { get; set; }

    /// <summary>
    /// External links to another site related to the media
    /// </summary>
    [JsonPropertyName("externalLinks")]
    public List<MediaExternalLink>? ExternalLinks { get; set; }

    /// <summary>
    /// The authenticated user's media list entry for the media
    /// </summary>
    [JsonPropertyName("mediaListEntry")]
    public MediaList? MediaListEntry { get; set; }

    /// <summary>
    /// User recommendations for similar media
    /// </summary>
    [JsonPropertyName("recommendations")]
    public RecommendationConnection? Recommendations { get; set; }
}

public class MediaTitle
{
    /// <summary>
    /// The romanization of the native language title
    /// </summary>
    [JsonPropertyName("romaji")]
    public string? Romaji { get; set; }

    /// <summary>
    /// The official english title
    /// </summary>
    [JsonPropertyName("english")]
    public string? English { get; set; }

    /// <summary>
    /// Official title in it's native language
    /// </summary>
    [JsonPropertyName("native")]
    public string? Native { get; set; }

    /// <summary>
    /// The currently authenticated users preferred title language. Default romaji for non-authenticated
    /// </summary>
    [JsonPropertyName("userPreferred")]
    public string? UserPreferred { get; set; }
}

public enum MediaType
{
    Anime,
    Manga
}

public enum MediaStatus
{
    Finished,
    Releasing,
    [EnumMember(Value = "Not_Yet_Released")]
    NotYetReleased,
    Cancelled,
    Hiatus
}

public class AiringSchedule
{
    /// <summary>
    /// The id of the airing schedule item
    /// </summary>
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    /// <summary>
    /// The time the episode airs at
    /// </summary>
    [JsonPropertyName("airingAt")]
    public int? AiringAt { get; set; }

    /// <summary>
    /// Seconds until episode starts airing
    /// </summary>
    [JsonPropertyName("timeUntilAiring")]
    public int? TimeUntilAiring { get; set; }

    /// <summary>
    /// The airing episode number
    /// </summary>
    [JsonPropertyName("episode")]
    public int? Episode { get; set; }

    /// <summary>
    /// The associate media id of the airing episode
    /// </summary>
    [JsonPropertyName("mediaId")]
    public int? MediaId { get; set; }

    /// <summary>
    /// The associate media of the airing episode
    /// </summary>
    [JsonPropertyName("media")]
    public Media? Media { get; set; }
}

public class MediaCoverImage
{
    /// <summary>
    /// The cover image url of the media at its largest size. If this size isn't available, large will be provided instead.
    /// </summary>
    [JsonPropertyName("extraLarge")]
    public string? ExtraLarge { get; set; }

    /// <summary>
    /// The cover image url of the media at a large size
    /// </summary>
    [JsonPropertyName("large")]
    public string? Large { get; set; }

    /// <summary>
    /// The cover image url of the media at medium size
    /// </summary>
    [JsonPropertyName("medium")]
    public string? Medium { get; set; }

    /// <summary>
    /// Average #hex color of cover image
    /// </summary>
    [JsonPropertyName("color")]
    public string? Color { get; set; }
}

public class MediaList
{
    /// <summary>
    /// The id of the list entry
    /// </summary>
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    /// <summary>
    /// The id of the user owner of the list entry
    /// </summary>
    [JsonPropertyName("userId")]
    public int? UserId { get; set; }

    /// <summary>
    /// The id of the media
    /// </summary>
    [JsonPropertyName("mediaId")]
    public int? MediaId { get; set; }

    /// <summary>
    /// The watching/reading status
    /// </summary>
    [JsonPropertyName("status")]
    public MediaListStatus? Status { get; set; }

    /// <summary>
    /// The score of the entry
    /// </summary>
    [JsonPropertyName("score")]
    public float? Score { get; set; }

    /// <summary>
    /// The amount of episodes/chapters consumed by the user
    /// </summary>
    [JsonPropertyName("progress")]
    public int? Progress { get; set; }

    /// <summary>
    /// The amount of volumes read by the user
    /// </summary>
    [JsonPropertyName("progressVolumes")]
    public int? ProgressVolumes { get; set; }

    /// <summary>
    /// The amount of times the user has rewatched/read the media
    /// </summary>
    [JsonPropertyName("repeat")]
    public int? Repeat { get; set; }

    /// <summary>
    /// Priority of planning
    /// </summary>
    [JsonPropertyName("priority")]
    public int? Priority { get; set; }

    /// <summary>
    /// If the entry should only be visible to authenticated user
    /// </summary>
    [JsonPropertyName("private")]
    public bool? IsPrivate { get; set; }

    /// <summary>
    /// Text notes
    /// </summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// If the entry shown be hidden from non-custom lists
    /// </summary>
    [JsonPropertyName("hiddenFromStatusLists")]
    public bool? HiddenFromStatusLists { get; set; }

    /// <summary>
    /// Map of booleans for which custom lists the entry are in
    /// </summary>
    [JsonPropertyName("customLists")]
    public Dictionary<string, bool>? CustomLists { get; set; }

    /// <summary>
    /// When the entry was started by the user
    /// </summary>
    [JsonPropertyName("startedAt")]
    public FuzzyDate? StartedAt { get; set; }

    /// <summary>
    /// When the entry was completed by the user
    /// </summary>
    [JsonPropertyName("completedAt")]
    public FuzzyDate? CompletedAt { get; set; }

    /// <summary>
    /// When the entry data was last updated
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public int? UpdatedAt { get; set; }

    /// <summary>
    /// When the entry data was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public int? CreatedAt { get; set; }

    [JsonPropertyName("media")]
    public Media? Media { get; set; }

    [JsonPropertyName("user")]
    public User? User { get; set; }
}

public enum MediaListStatus
{
    Current,
    Planning,
    Completed,
    Dropped,
    Paused,
    Repeating
}

public enum MediaSource
{
    Original,
    Manga,
    [EnumMember(Value = "Light_Novel")]
    LightNovel,
    [EnumMember(Value = "Visual_Novel")]
    VisualNovel,
    [EnumMember(Value = "Video_Game")]
    VideoGame,
    Other,
    Novel,
    Doujinshi,
    Anime,
    [EnumMember(Value = "Web_Novel")]
    WebNovel,
    [EnumMember(Value = "Live_Action")]
    LiveAction,
    Game,
    Comic,
    [EnumMember(Value = "Multimedia_Project")]
    MultimediaProject,
    [EnumMember(Value = "Picture_Book")]
    PictureBook
}

public enum MediaFormat
{
    Tv,
    [EnumMember(Value = "Tv_Short")]
    TvShort,
    Movie,
    Special,
    Ova,
    Ona,
    Music,
    Manga,
    Novel,
    [EnumMember(Value = "One_Shot")]
    OneShot
}

public class MediaTrailer
{
    /// <summary>
    /// The trailer video id
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The site the video is hosted by (Currently either youtube or dailymotion)
    /// </summary>
    [JsonPropertyName("site")]
    public string? Site { get; set; }

    /// <summary>
    /// The url for the thumbnail image of the video
    /// </summary>
    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }
}

public class MediaTagCollection
{
    [JsonPropertyName("tags")]
    public List<MediaTag>? Tags { get; set; }
}

public class MediaTag
{
    /// <summary>
    /// The id of the tag
    /// </summary>
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    /// <summary>
    /// The name of the tag
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// A general description of the tag
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The categories of tags this tag belongs to
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>
    /// The relevance ranking of the tag out of the 100 for this media
    /// </summary>
    [JsonPropertyName("rank")]
    public int? Rank { get; set; }

    /// <summary>
    /// If the tag could be a spoiler for any media
    /// </summary>
    [JsonPropertyName("isGeneralSpoiler")]
    public bool? IsGeneralSpoiler { get; set; }

    /// <summary>
    /// If the tag is a spoiler for this media
    /// </summary>
    [JsonPropertyName("isMediaSpoiler")]
    public bool? IsMediaSpoiler { get; set; }

    /// <summary>
    /// If the tag is only for adult 18+ media
    /// </summary>
    [JsonPropertyName("isAdult")]
    public bool? IsAdult { get; set; }

    /// <summary>
    /// The user who submitted the tag
    /// </summary>
    [JsonPropertyName("userId")]
    public int? UserId { get; set; }
}

public class MediaConnection
{
    [JsonPropertyName("edges")]
    public List<MediaEdge>? Edges { get; set; }

    [JsonPropertyName("nodes")]
    public List<Media>? Nodes { get; set; }

    /// <summary>
    /// The pagination information
    /// </summary>
    [JsonPropertyName("pageInfo")]
    public PageInfo? PageInfo { get; set; }
}

public class MediaEdge
{
    public Media? Node { get; set; }

    /// <summary>
    /// The id of the connection
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// The type of relation to the parent model
    /// </summary>
    public MediaRelation? RelationType { get; set; }

    /// <summary>
    /// If the studio is the main animation studio of the media (For Studio->MediaConnection field only)
    /// </summary>
    public bool? IsMainStudio { get; set; }

    /// <summary>
    /// The characters in the media voiced by the parent actor
    /// </summary>
    public List<Character>? Characters { get; set; }

    /// <summary>
    /// The characters role in the media
    /// </summary>
    public string? CharacterRole { get; set; }

    /// <summary>
    /// Media specific character name
    /// </summary>
    public string? CharacterName { get; set; }

    /// <summary>
    /// Notes regarding the VA's role for the character
    /// </summary>
    public string? RoleNotes { get; set; }

    /// <summary>
    /// Used for grouping roles where multiple dubs exist for the same language. Either dubbing company name or language variant.
    /// </summary>
    public string? DubGroup { get; set; }

    /// <summary>
    /// The role of the staff member in the production of the media
    /// </summary>
    public string? StaffRole { get; set; }

    /// <summary>
    /// The order the media should be displayed from the users favourites
    /// </summary>
    public int? FavouriteOrder { get; set; }
}

public enum MediaRelation
{
    Adaptation,
    Prequel,
    Sequel,
    Parent,
    [EnumMember(Value = "Side_Story")]
    SideStory,
    Character,
    Summary,
    Alternative,
    [EnumMember(Value = "Spin_Off")]
    SpinOff,
    Other,
    Source,
    Compilation,
    Contains
}

public enum MediaSeason
{
    Winter,
    Spring,
    Summer,
    Fall
}

public class MediaExternalLink
{
    /// <summary>
    /// The id of the external link
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// The url of the external link or base url of link source
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// The links website site name
    /// </summary>
    public string? Site { get; set; }

    /// <summary>
    /// The links website site id
    /// </summary>
    public int? SiteId { get; set; }

    /// <summary>
    /// Language the site content is in. See Staff language field for values.
    /// </summary>
    public string? Language { get; set; }

    public string? Color { get; set; }

    /// <summary>
    /// The icon image url of the site. Not available for all links. Transparent PNG 64x64
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// isDisabled: Boolean
    /// </summary>
    public string? Notes { get; set; }
}

public enum ExternalLinkType
{
    Info,
    Streaming,
    Social
}

public class MediaListCollection
{
    /// <summary>
    /// Grouped media list entries
    /// </summary>
    public List<MediaListGroup>? Lists { get; set; }

    /// <summary>
    /// The owner of the list
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// If there is another chunk
    /// </summary>
    public bool? HasNextChunk { get; set; }
}

public class MediaListGroup
{
    /// <summary>
    /// Media list entries
    /// </summary>
    public List<MediaList>? Entries { get; set; }

    public string? Name { get; set; }

    public bool? IsCustomList { get; set; }

    public bool? IsSplitCompletedList { get; set; }

    public MediaListStatus? Status { get; set; }
}

public class QueryMedia
{
    public Media? Media { get; set; }
}