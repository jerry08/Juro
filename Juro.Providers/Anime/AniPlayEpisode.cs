using System.Text.Json.Serialization;

namespace Juro.Providers.Anime;

public class AniPlayEpisode
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("hasDub")]
    public bool HasDub { get; set; }

    [JsonPropertyName("isFiller")]
    public bool IsFiller { get; set; }

    [JsonPropertyName("dubId")]
    public string? DubId { get; set; }

    [JsonPropertyName("img")]
    public string? Image { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    public override string ToString() => $"Episode {Number}";
}
