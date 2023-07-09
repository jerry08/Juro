using System.Text.Json.Serialization;

namespace Juro.Providers.Anilist.Api
{
    public class FuzzyDate
    {
        [JsonPropertyName("year")] public int? Year { get; set; }

        [JsonPropertyName("month")] public int? Month { get; set; }

        [JsonPropertyName("day")] public int? Day { get; set; }
    }
}