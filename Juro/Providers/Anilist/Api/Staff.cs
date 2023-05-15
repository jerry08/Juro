using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Juro.Providers.Anilist.Api;

public class Staff
{
    /// <summary>
    /// The id of the staff member
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// The primary language of the staff member. Current values: Japanese, English, Korean, Italian, Spanish, Portuguese, French, German, Hebrew, Hungarian, Chinese, Arabic, Filipino, Catalan, Finnish, Turkish, Dutch, Swedish, Thai, Tagalog, Malaysian, Indonesian, Vietnamese, Nepali, Hindi, Urdu
    /// </summary>
    [JsonPropertyName("languageV2")]
    public string? LanguageV2 { get; set; }

    /// <summary>
    /// A general description of the staff member
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The person's primary occupations
    /// </summary>
    [JsonPropertyName("primaryOccupations")]
    public List<string>? PrimaryOccupations { get; set; }

    /// <summary>
    /// The staff's gender. Usually Male, Female, or Non-binary but can be any string.
    /// </summary>
    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("dateOfBirth")]
    public FuzzyDate? DateOfBirth { get; set; }

    [JsonPropertyName("dateOfDeath")]
    public FuzzyDate? DateOfDeath { get; set; }

    /// <summary>
    /// The person's age in years
    /// </summary>
    [JsonPropertyName("age")]
    public int? Age { get; set; }

    /// <summary>
    /// [startYear, endYear] (If the 2nd value is not present staff is still active)
    /// </summary>
    [JsonPropertyName("yearsActive")]
    public List<int>? YearsActive { get; set; }

    /// <summary>
    /// The persons birthplace or hometown
    /// </summary>
    [JsonPropertyName("homeTown")]
    public string? HomeTown { get; set; }

    /// <summary>
    /// The persons blood type
    /// </summary>
    [JsonPropertyName("bloodType")]
    public string? BloodType { get; set; }

    /// <summary>
    /// If the staff member is marked as favourite by the currently authenticated user
    /// </summary>
    [JsonPropertyName("isFavourite")]
    public bool? IsFavourite { get; set; }

    /// <summary>
    /// If the staff member is blocked from being added to favourites
    /// </summary>
    [JsonPropertyName("isFavouriteBlocked")]
    public bool? IsFavouriteBlocked { get; set; }

    /// <summary>
    /// The url for the staff page on the AniList website
    /// </summary>
    [JsonPropertyName("siteUrl")]
    public string? SiteUrl { get; set; }

    /// <summary>
    /// Media where the staff member has a production role
    /// </summary>
    [JsonPropertyName("staffMedia")]
    public MediaConnection? StaffMedia { get; set; }

    /// <summary>
    /// Characters voiced by the actor
    /// </summary>
    [JsonPropertyName("characters")]
    public CharacterConnection? Characters { get; set; }

    /// <summary>
    /// Media the actor voiced characters in. (Same data as characters with media as node instead of characters)
    /// </summary>
    [JsonPropertyName("characterMedia")]
    public MediaConnection? CharacterMedia { get; set; }

    /// <summary>
    /// Staff member that the submission is referencing
    /// </summary>
    [JsonPropertyName("staff")]
    public Staff? StaffMember { get; set; }

    /// <summary>
    /// Submitter for the submission
    /// </summary>
    [JsonPropertyName("submitter")]
    public User? Submitter { get; set; }

    /// <summary>
    /// Status of the submission
    /// </summary>
    [JsonPropertyName("submissionStatus")]
    public int? SubmissionStatus { get; set; }

    /// <summary>
    /// Inner details of submission status
    /// </summary>
    [JsonPropertyName("submissionNotes")]
    public string? SubmissionNotes { get; set; }

    /// <summary>
    /// The amount of user's who have favourited the staff member
    /// </summary>
    [JsonPropertyName("favourites")]
    public int? Favourites { get; set; }

    /// <summary>
    /// Notes for site moderators
    /// </summary>
    [JsonPropertyName("modNotes")]
    public string? ModNotes { get; set; }
}

public class StaffConnection
{
    /// <summary>
    /// Notes for site moderators
    /// </summary>
    [JsonPropertyName("nodes")]
    public List<Staff>? Nodes { get; set; }
}