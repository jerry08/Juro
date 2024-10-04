using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Juro.DataBuilder.Models;

public class License
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("url")]
    public string Url { get; set; } = default!;
}

public class Root
{
    [JsonPropertyName("license")]
    public License License { get; set; } = default!;

    [JsonPropertyName("repository")]
    public string Repository { get; set; } = default!;

    [JsonPropertyName("lastUpdate")]
    public string LastUpdate { get; set; } = default!;

    [JsonPropertyName("data")]
    public List<ManamiAnimeItem> Data { get; set; } = [];
}

public class ManamiAnimeItem
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    [JsonPropertyName("sources")]
    public List<string> Sources { get; set; } = [];

    [JsonPropertyName("title")]
    public string Title { get; set; } = default!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;

    [JsonPropertyName("episodes")]
    public int Episodes { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("animeSeason")]
    public AnimeSeason AnimeSeason { get; set; } = default!;

    [JsonPropertyName("picture")]
    public string Picture { get; set; } = default!;

    [JsonPropertyName("thumbnail")]
    public string Thumbnail { get; set; } = default!;

    [JsonPropertyName("synonyms")]
    public List<string> Synonyms { get; set; } = [];

    [JsonPropertyName("relatedAnime")]
    public List<string> RelatedAnime { get; set; } = [];

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    public string? GogoanimeId { get; set; }
    public string? AnimePaheId { get; set; }
    public string? KaidoId { get; set; }
    public string? AniwaveId { get; set; }
    public string? OtakuDesuId { get; set; }
}

public class AnimeSeason
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    [JsonPropertyName("season")]
    public string Season { get; set; } = default!;

    [JsonPropertyName("year")]
    public int Year { get; set; }
}