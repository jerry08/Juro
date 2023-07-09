using System.Runtime.Serialization;

namespace Juro.Providers.Aniskip
{
    public class Stamp
    {
        public AniSkipInterval Interval { get; set; } = default!;

        public SkipType SkipType { get; set; }

        public string SkipId { get; set; } = default!;

        public double EpisodeLength { get; set; }
    }

    public enum SkipType
    {
        [EnumMember(Value = "op")] Opening,
        [EnumMember(Value = "ed")] Ending,
        [EnumMember(Value = "recap")] Recap,
        [EnumMember(Value = "mixed-op")] MixedOpening,
        [EnumMember(Value = "mixed-ed")] MixedEnding
    }
}