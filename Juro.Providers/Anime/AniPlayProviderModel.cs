using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Juro.Providers.Anime;

public class AniPlayProviderModel
{
    [JsonPropertyName("episodes")]
    public List<AniPlayEpisode> Episodes { get; set; } = [];

    [JsonPropertyName("providerId")]
    public string ProviderId { get; set; } = default!;

    [JsonPropertyName("default")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("dub")]
    public bool IsDub { get; set; }
}
