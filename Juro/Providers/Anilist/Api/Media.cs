using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Juro.Providers.Anilist.Api;

public class Media
{
    /// <summary>
    /// The id of the media
    /// </summary>
    [JsonProperty("id")]
    public int Id { get; set; }

    /// <summary>
    /// The mal id of the media
    /// </summary>
    [JsonProperty("idMal")]
    public int? IdMal { get; set; }

    /// <summary>
    /// The official titles of the media in various languages
    /// </summary>
    [JsonProperty("title")]
    public MediaTitle? Title { get; set; }

    /// <summary>
    /// The type of the media; anime or manga
    /// </summary>
    [JsonProperty("type")]
    public MediaType? Type { get; set; }

    /// <summary>
    /// The format the media was released in
    /// </summary>
    [JsonProperty("format")]
    public MediaFormat? Format { get; set; }

    /// <summary>
    /// The current releasing status of the media
    /// </summary>
    [JsonProperty("status")]
    public MediaStatus? Status { get; set; }

    /// <summary>
    /// Short description of the media's story and characters
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The first official release date of the media
    /// </summary>
    [JsonProperty("startDate")]
    public FuzzyDate? StartDate { get; set; }

    /// <summary>
    /// The last official release date of the media
    /// </summary>
    [JsonProperty("endDate")]
    public FuzzyDate? EndDate { get; set; }

    /// <summary>
    /// The season the media was initially released in
    /// </summary>
    [JsonProperty("season")]
    public MediaSeason? Season { get; set; }

    /// <summary>
    /// The season year the media was initially released in
    /// </summary>
    [JsonProperty("seasonYear")]
    public int? SeasonYear { get; set; }

    /// <summary>
    /// The year &#38; season the media was initially released in
    /// </summary>
    [JsonProperty("seasonInt")]
    public int? SeasonInt { get; set; }

    /// <summary>
    /// The amount of episodes the anime has when complete
    /// </summary>
    [JsonProperty("episodes")]
    public int? Episodes { get; set; }

    /// <summary>
    /// The general length of each anime episode in minutes
    /// </summary>
    [JsonProperty("duration")]
    public int? Duration { get; set; }

    /// <summary>
    /// The amount of chapters the manga has when complete
    /// </summary>
    [JsonProperty("chapters")]
    public int? Chapters { get; set; }

    /// <summary>
    /// The amount of volumes the manga has when complete
    /// </summary>
    [JsonProperty("volumes")]
    public int? Volumes { get; set; }

    /// <summary>
    /// Where the media was created. (ISO 3166-1 alpha-2)
    /// Originally a "CountryCode"
    /// </summary>
    [JsonProperty("countryOfOrigin")]
    public string? CountryOfOrigin { get; set; }

    /// <summary>
    /// Source type the media was adapted from.
    /// </summary>
    [JsonProperty("source")]
    public MediaSource? Source { get; set; }

    /// <summary>
    /// Official Twitter hashtags for the media
    /// </summary>
    [JsonProperty("hashtag")]
    public string? Hashtag { get; set; }

    /// <summary>
    /// Media trailer or advertisement
    /// </summary>
    [JsonProperty("trailer")]
    public MediaTrailer? Trailer { get; set; }

    /// <summary>
    /// When the media's data was last updated
    /// </summary>
    [JsonProperty("updatedAt")]
    public int? UpdatedAt { get; set; }

    /// <summary>
    /// The cover images of the media
    /// </summary>
    [JsonProperty("coverImage")]
    public MediaCoverImage? CoverImage { get; set; }

    /// <summary>
    /// The banner image of the media
    /// </summary>
    [JsonProperty("bannerImage")]
    public string? BannerImage { get; set; }

    /// <summary>
    /// The genres of the media
    /// </summary>
    [JsonProperty("genres")]
    public List<string>? Genres { get; set; }

    /// <summary>
    /// Alternative titles of the media
    /// </summary>
    [JsonProperty("synonyms")]
    public List<string>? Synonyms { get; set; }

    /// <summary>
    /// A weighted average score of all the user's scores of the media
    /// </summary>
    [JsonProperty("averageScore")]
    public int? AverageScore { get; set; }

    /// <summary>
    /// Mean score of all the user's scores of the media
    /// </summary>
    [JsonProperty("meanScore")]
    public int? MeanScore { get; set; }

    /// <summary>
    /// The number of users with the media on their list
    /// </summary>
    [JsonProperty("popularity")]
    public int? Popularity { get; set; }

    /// <summary>
    /// Locked media may not be added to lists our favorited. This may be due to the entry pending for deletion or other reasons.
    /// </summary>
    [JsonProperty("isLocked")]
    public bool? IsLocked { get; set; }

    /// <summary>
    /// The amount of related activity in the past hour
    /// </summary>
    [JsonProperty("trending")]
    public int? Trending { get; set; }

    /// <summary>
    /// The amount of user's who have favourited the media
    /// </summary>
    [JsonProperty("favourites")]
    public int? Favourites { get; set; }

    /// <summary>
    /// List of tags that describes elements and themes of the media
    /// </summary>
    [JsonProperty("tags")]
    public List<MediaTag>? Tags { get; set; }

    /// <summary>
    /// Other media in the same or connecting franchise
    /// </summary>
    [JsonProperty("relations")]
    public MediaConnection? Relations { get; set; }

    /// <summary>
    /// The characters in the media
    /// </summary>
    [JsonProperty("characters")]
    public CharacterConnection? Characters { get; set; }

    /// <summary>
    /// The companies who produced the media
    /// </summary>
    [JsonProperty("studios")]
    public StudioConnection? Studios { get; set; }

    /// <summary>
    /// If the media is marked as favourite by the current authenticated user
    /// </summary>
    [JsonProperty("isFavourite")]
    public bool? IsFavourite { get; set; }

    /// <summary>
    /// If the media is blocked from being added to favourites
    /// </summary>
    [JsonProperty("isFavouriteBlocked")]
    public bool? IsFavouriteBlocked { get; set; }

    /// <summary>
    /// If the media is intended only for 18+ adult audiences
    /// </summary>
    [JsonProperty("isAdult")]
    public bool? IsAdult { get; set; }

    /// <summary>
    /// The media's next episode airing schedule
    /// </summary>
    [JsonProperty("nextAiringEpisode")]
    public AiringSchedule? NextAiringEpisode { get; set; }

    /// <summary>
    /// External links to another site related to the media
    /// </summary>
    [JsonProperty("externalLinks")]
    public List<MediaExternalLink>? ExternalLinks { get; set; }

    /// <summary>
    /// The authenticated user's media list entry for the media
    /// </summary>
    [JsonProperty("mediaListEntry")]
    public MediaList? MediaListEntry { get; set; }

    /// <summary>
    /// User recommendations for similar media
    /// </summary>
    [JsonProperty("recommendations")]
    public RecommendationConnection? Recommendations { get; set; }
}

public class MediaTitle
{
    /// <summary>
    /// The romanization of the native language title
    /// </summary>
    [JsonProperty("romaji")]
    public string? Romaji { get; set; }

    /// <summary>
    /// The official english title
    /// </summary>
    [JsonProperty("english")]
    public string? English { get; set; }

    /// <summary>
    /// Official title in it's native language
    /// </summary>
    [JsonProperty("native")]
    public string? Native { get; set; }

    /// <summary>
    /// The currently authenticated users preferred title language. Default romaji for non-authenticated
    /// </summary>
    [JsonProperty("userPreferred")]
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
    [JsonProperty("id")]
    public int? Id { get; set; }

    /// <summary>
    /// The time the episode airs at
    /// </summary>
    [JsonProperty("airingAt")]
    public int? AiringAt { get; set; }

    /// <summary>
    /// Seconds until episode starts airing
    /// </summary>
    [JsonProperty("timeUntilAiring")]
    public int? TimeUntilAiring { get; set; }

    /// <summary>
    /// The airing episode number
    /// </summary>
    [JsonProperty("episode")]
    public int? Episode { get; set; }

    /// <summary>
    /// The associate media id of the airing episode
    /// </summary>
    [JsonProperty("mediaId")]
    public int? MediaId { get; set; }

    /// <summary>
    /// The associate media of the airing episode
    /// </summary>
    [JsonProperty("media")]
    public Media? Media { get; set; }
}

public class MediaCoverImage
{
    /// <summary>
    /// The cover image url of the media at its largest size. If this size isn't available, large will be provided instead.
    /// </summary>
    [JsonProperty("extraLarge")]
    public string? ExtraLarge { get; set; }

    /// <summary>
    /// The cover image url of the media at a large size
    /// </summary>
    [JsonProperty("large")]
    public string? Large { get; set; }

    /// <summary>
    /// The cover image url of the media at medium size
    /// </summary>
    [JsonProperty("medium")]
    public string? Medium { get; set; }

    /// <summary>
    /// Average #hex color of cover image
    /// </summary>
    [JsonProperty("color")]
    public string? Color { get; set; }
}

public class MediaList
{
    /// <summary>
    /// The id of the list entry
    /// </summary>
    [JsonProperty("id")]
    public int? Id { get; set; }

    /// <summary>
    /// The id of the user owner of the list entry
    /// </summary>
    [JsonProperty("userId")]
    public int? UserId { get; set; }

    /// <summary>
    /// The id of the media
    /// </summary>
    [JsonProperty("mediaId")]
    public int? MediaId { get; set; }

    /// <summary>
    /// The watching/reading status
    /// </summary>
    [JsonProperty("status")]
    public MediaListStatus? Status { get; set; }

    /// <summary>
    /// The score of the entry
    /// </summary>
    [JsonProperty("score")]
    public float? Score { get; set; }

    /// <summary>
    /// The amount of episodes/chapters consumed by the user
    /// </summary>
    [JsonProperty("progress")]
    public int? Progress { get; set; }

    /// <summary>
    /// The amount of volumes read by the user
    /// </summary>
    [JsonProperty("progressVolumes")]
    public int? ProgressVolumes { get; set; }

    /// <summary>
    /// The amount of times the user has rewatched/read the media
    /// </summary>
    [JsonProperty("repeat")]
    public int? Repeat { get; set; }

    /// <summary>
    /// Priority of planning
    /// </summary>
    [JsonProperty("priority")]
    public int? Priority { get; set; }

    /// <summary>
    /// If the entry should only be visible to authenticated user
    /// </summary>
    [JsonProperty("private")]
    public bool? IsPrivate { get; set; }

    /// <summary>
    /// Text notes
    /// </summary>
    [JsonProperty("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// If the entry shown be hidden from non-custom lists
    /// </summary>
    [JsonProperty("hiddenFromStatusLists")]
    public bool? HiddenFromStatusLists { get; set; }

    /// <summary>
    /// Map of booleans for which custom lists the entry are in
    /// </summary>
    [JsonProperty("customLists")]
    public Dictionary<string, bool>? CustomLists { get; set; }

    /// <summary>
    /// When the entry was started by the user
    /// </summary>
    [JsonProperty("startedAt")]
    public FuzzyDate? StartedAt { get; set; }

    /// <summary>
    /// When the entry was completed by the user
    /// </summary>
    [JsonProperty("completedAt")]
    public FuzzyDate? CompletedAt { get; set; }

    /// <summary>
    /// When the entry data was last updated
    /// </summary>
    [JsonProperty("updatedAt")]
    public int? UpdatedAt { get; set; }

    /// <summary>
    /// When the entry data was created
    /// </summary>
    [JsonProperty("createdAt")]
    public int? CreatedAt { get; set; }

    [JsonProperty("media")]
    public Media? Media { get; set; }

    [JsonProperty("user")]
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
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The site the video is hosted by (Currently either youtube or dailymotion)
    /// </summary>
    [JsonProperty("site")]
    public string? Site { get; set; }

    /// <summary>
    /// The url for the thumbnail image of the video
    /// </summary>
    [JsonProperty("thumbnail")]
    public string? Thumbnail { get; set; }
}

public class MediaTagCollection
{
    [JsonProperty("tags")]
    public List<MediaTag>? Tags { get; set; }
}

public class MediaTag
{
    /// <summary>
    /// The id of the tag
    /// </summary>
    [JsonProperty("id")]
    public int? Id { get; set; }

    /// <summary>
    /// The name of the tag
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// A general description of the tag
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The categories of tags this tag belongs to
    /// </summary>
    [JsonProperty("category")]
    public string? Category { get; set; }

    /// <summary>
    /// The relevance ranking of the tag out of the 100 for this media
    /// </summary>
    [JsonProperty("rank")]
    public int? Rank { get; set; }

    /// <summary>
    /// If the tag could be a spoiler for any media
    /// </summary>
    [JsonProperty("isGeneralSpoiler")]
    public bool? IsGeneralSpoiler { get; set; }

    /// <summary>
    /// If the tag is a spoiler for this media
    /// </summary>
    [JsonProperty("isMediaSpoiler")]
    public bool? IsMediaSpoiler { get; set; }

    /// <summary>
    /// If the tag is only for adult 18+ media
    /// </summary>
    [JsonProperty("isAdult")]
    public bool? IsAdult { get; set; }

    /// <summary>
    /// The user who submitted the tag
    /// </summary>
    [JsonProperty("userId")]
    public int? UserId { get; set; }
}

public class MediaConnection
{
    [JsonProperty("edges")]
    public List<MediaEdge>? Edges { get; set; }

    [JsonProperty("nodes")]
    public List<Media>? Nodes { get; set; }

    /// <summary>
    /// The pagination information
    /// </summary>
    [JsonProperty("pageInfo")]
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