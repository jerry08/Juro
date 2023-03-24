using System.Collections.Generic;
using Newtonsoft.Json;

namespace Juro.Providers.Anilist.Api;

public class Staff
{
    /// <summary>
    /// The id of the staff member
    /// </summary>
    [JsonProperty("id")]
    public int Id { get; set; }

    /// <summary>
    /// The primary language of the staff member. Current values: Japanese, English, Korean, Italian, Spanish, Portuguese, French, German, Hebrew, Hungarian, Chinese, Arabic, Filipino, Catalan, Finnish, Turkish, Dutch, Swedish, Thai, Tagalog, Malaysian, Indonesian, Vietnamese, Nepali, Hindi, Urdu
    /// </summary>
    [JsonProperty("languageV2")]
    public string? LanguageV2 { get; set; }

    /// <summary>
    /// A general description of the staff member
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The person's primary occupations
    /// </summary>
    [JsonProperty("primaryOccupations")]
    public List<string>? PrimaryOccupations { get; set; }

    /// <summary>
    /// The staff's gender. Usually Male, Female, or Non-binary but can be any string.
    /// </summary>
    [JsonProperty("gender")]
    public string? Gender { get; set; }

    [JsonProperty("dateOfBirth")]
    public FuzzyDate? DateOfBirth { get; set; }

    [JsonProperty("dateOfDeath")]
    public FuzzyDate? DateOfDeath { get; set; }

    /// <summary>
    /// The person's age in years
    /// </summary>
    [JsonProperty("age")]
    public int? Age { get; set; }

    /// <summary>
    /// [startYear, endYear] (If the 2nd value is not present staff is still active)
    /// </summary>
    [JsonProperty("yearsActive")]
    public List<int>? YearsActive { get; set; }

    /// <summary>
    /// The persons birthplace or hometown
    /// </summary>
    [JsonProperty("homeTown")]
    public string? HomeTown { get; set; }

    /// <summary>
    /// The persons blood type
    /// </summary>
    [JsonProperty("bloodType")]
    public string? BloodType { get; set; }

    /// <summary>
    /// If the staff member is marked as favourite by the currently authenticated user
    /// </summary>
    [JsonProperty("isFavourite")]
    public bool? IsFavourite { get; set; }

    /// <summary>
    /// If the staff member is blocked from being added to favourites
    /// </summary>
    [JsonProperty("isFavouriteBlocked")]
    public bool? IsFavouriteBlocked { get; set; }

    /// <summary>
    /// The url for the staff page on the AniList website
    /// </summary>
    [JsonProperty("siteUrl")]
    public string? SiteUrl { get; set; }

    /// <summary>
    /// Media where the staff member has a production role
    /// </summary>
    [JsonProperty("staffMedia")]
    public MediaConnection? StaffMedia { get; set; }

    /// <summary>
    /// Characters voiced by the actor
    /// </summary>
    [JsonProperty("characters")]
    public CharacterConnection? Characters { get; set; }

    /// <summary>
    /// Media the actor voiced characters in. (Same data as characters with media as node instead of characters)
    /// </summary>
    [JsonProperty("characterMedia")]
    public MediaConnection? CharacterMedia { get; set; }

    /// <summary>
    /// Staff member that the submission is referencing
    /// </summary>
    [JsonProperty("staff")]
    public Staff? StaffMember { get; set; }

    /// <summary>
    /// Submitter for the submission
    /// </summary>
    [JsonProperty("submitter")]
    public User? Submitter { get; set; }

    /// <summary>
    /// Status of the submission
    /// </summary>
    [JsonProperty("submissionStatus")]
    public int? SubmissionStatus { get; set; }

    /// <summary>
    /// Inner details of submission status
    /// </summary>
    [JsonProperty("submissionNotes")]
    public string? SubmissionNotes { get; set; }

    /// <summary>
    /// The amount of user's who have favourited the staff member
    /// </summary>
    [JsonProperty("favourites")]
    public int? Favourites { get; set; }

    /// <summary>
    /// Notes for site moderators
    /// </summary>
    [JsonProperty("modNotes")]
    public string? ModNotes { get; set; }
}

public class StaffConnection
{
    /// <summary>
    /// Notes for site moderators
    /// </summary>
    [JsonProperty("nodes")]
    public List<Staff>? Nodes { get; set; }
}