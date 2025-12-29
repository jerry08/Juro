using System.Collections.Generic;

namespace Juro.Providers.Anime.Zoro;

/// <summary>
/// Search parameters for ZoroTheme-based providers.
/// </summary>
public class ZoroThemeSearchParameters
{
    /// <summary>
    /// Page number for pagination.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Anime type filter (e.g., "1" for TV, "2" for Movie).
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Airing status filter (e.g., "1" for Airing, "2" for Completed).
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Rating filter (e.g., "pg-13", "r").
    /// </summary>
    public string? Rated { get; set; }

    /// <summary>
    /// Score filter (e.g., "8" for 8+ rating).
    /// </summary>
    public string? Score { get; set; }

    /// <summary>
    /// Season filter (e.g., "spring", "summer", "fall", "winter").
    /// </summary>
    public string? Season { get; set; }

    /// <summary>
    /// Language filter (e.g., "sub", "dub").
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Sort order (e.g., "default", "recently_added", "recently_updated", "score", "name_az", "name_za").
    /// </summary>
    public string? Sort { get; set; }

    /// <summary>
    /// Start year filter.
    /// </summary>
    public string? StartYear { get; set; }

    /// <summary>
    /// Start month filter.
    /// </summary>
    public string? StartMonth { get; set; }

    /// <summary>
    /// Start day filter.
    /// </summary>
    public string? StartDay { get; set; }

    /// <summary>
    /// End year filter.
    /// </summary>
    public string? EndYear { get; set; }

    /// <summary>
    /// End month filter.
    /// </summary>
    public string? EndMonth { get; set; }

    /// <summary>
    /// End day filter.
    /// </summary>
    public string? EndDay { get; set; }

    /// <summary>
    /// Comma-separated genre IDs.
    /// </summary>
    public string? Genres { get; set; }
}

/// <summary>
/// Available filter options for ZoroTheme providers.
/// </summary>
public static class ZoroThemeFilters
{
    public static readonly Dictionary<string, string> TypeOptions = new()
    {
        ["All"] = "",
        ["TV"] = "1",
        ["Movie"] = "2",
        ["OVA"] = "3",
        ["ONA"] = "4",
        ["Special"] = "5",
        ["Music"] = "6",
    };

    public static readonly Dictionary<string, string> StatusOptions = new()
    {
        ["All"] = "",
        ["Airing"] = "1",
        ["Completed"] = "2",
        ["Upcoming"] = "3",
    };

    public static readonly Dictionary<string, string> RatedOptions = new()
    {
        ["All"] = "",
        ["G"] = "g",
        ["PG"] = "pg",
        ["PG-13"] = "pg-13",
        ["R"] = "r",
        ["R+"] = "r+",
        ["Rx"] = "rx",
    };

    public static readonly Dictionary<string, string> ScoreOptions = new()
    {
        ["All"] = "",
        ["(1) Appalling"] = "1",
        ["(2) Horrible"] = "2",
        ["(3) Very Bad"] = "3",
        ["(4) Bad"] = "4",
        ["(5) Average"] = "5",
        ["(6) Fine"] = "6",
        ["(7) Good"] = "7",
        ["(8) Very Good"] = "8",
        ["(9) Great"] = "9",
        ["(10) Masterpiece"] = "10",
    };

    public static readonly Dictionary<string, string> SeasonOptions = new()
    {
        ["All"] = "",
        ["Spring"] = "spring",
        ["Summer"] = "summer",
        ["Fall"] = "fall",
        ["Winter"] = "winter",
    };

    public static readonly Dictionary<string, string> LanguageOptions = new()
    {
        ["All"] = "",
        ["Sub"] = "sub",
        ["Dub"] = "dub",
        ["Sub & Dub"] = "sub-dub",
    };

    public static readonly Dictionary<string, string> SortOptions = new()
    {
        ["Default"] = "default",
        ["Recently Added"] = "recently_added",
        ["Recently Updated"] = "recently_updated",
        ["Score"] = "score",
        ["Name A-Z"] = "name_az",
        ["Name Z-A"] = "name_za",
        ["Released Date"] = "released_date",
        ["Most Watched"] = "most_watched",
    };

    public static readonly Dictionary<string, string> GenreOptions = new()
    {
        ["Action"] = "1",
        ["Adventure"] = "2",
        ["Cars"] = "3",
        ["Comedy"] = "4",
        ["Dementia"] = "5",
        ["Demons"] = "6",
        ["Drama"] = "8",
        ["Ecchi"] = "9",
        ["Fantasy"] = "10",
        ["Game"] = "11",
        ["Harem"] = "35",
        ["Historical"] = "13",
        ["Horror"] = "14",
        ["Isekai"] = "44",
        ["Josei"] = "43",
        ["Kids"] = "15",
        ["Magic"] = "16",
        ["Martial Arts"] = "17",
        ["Mecha"] = "18",
        ["Military"] = "38",
        ["Music"] = "19",
        ["Mystery"] = "7",
        ["Parody"] = "20",
        ["Police"] = "39",
        ["Psychological"] = "40",
        ["Romance"] = "22",
        ["Samurai"] = "21",
        ["School"] = "23",
        ["Sci-Fi"] = "24",
        ["Seinen"] = "42",
        ["Shoujo"] = "25",
        ["Shoujo Ai"] = "26",
        ["Shounen"] = "27",
        ["Shounen Ai"] = "28",
        ["Slice of Life"] = "36",
        ["Space"] = "29",
        ["Sports"] = "30",
        ["Super Power"] = "31",
        ["Supernatural"] = "37",
        ["Thriller"] = "41",
        ["Vampire"] = "32",
        ["Yaoi"] = "33",
        ["Yuri"] = "34",
    };
}
