using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Juro.Providers.Aniskip;

public class AniSkipResponse
{
    [JsonPropertyName("found")]
    public bool IsFound { get; set; }

    public List<Stamp>? Results { get; set; }

    public string? Message { get; set; }

    public int StatusCode { get; set; }
}