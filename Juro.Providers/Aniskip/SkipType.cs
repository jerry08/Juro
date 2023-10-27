using System.Runtime.Serialization;

namespace Juro.Providers.Aniskip;

public enum SkipType
{
    [EnumMember(Value = "op")]
    Opening,

    [EnumMember(Value = "ed")]
    Ending,

    [EnumMember(Value = "recap")]
    Recap,

    [EnumMember(Value = "mixed-op")]
    MixedOpening,

    [EnumMember(Value = "mixed-ed")]
    MixedEnding,
}