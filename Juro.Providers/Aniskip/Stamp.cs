using System.Text.Json.Serialization;
using JsonStringEnumConverter = Juro.Core.Converters.JsonStringEnumConverter;

namespace Juro.Providers.Aniskip;

public class Stamp
{
    public AniSkipInterval Interval { get; set; } = default!;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SkipType SkipType { get; set; }

    public string SkipId { get; set; } = default!;

    public double EpisodeLength { get; set; }
}
