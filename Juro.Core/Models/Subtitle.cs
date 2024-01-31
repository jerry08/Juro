using System.Collections.Generic;

namespace Juro.Core.Models;

public class Subtitle
{
    public string Url { get; set; } = default!;

    public string Language { get; set; } = default!;

    public SubtitleType Type { get; set; } = SubtitleType.VTT;

    public Dictionary<string, string> Headers { get; set; } = [];

    public Subtitle() { }

    public Subtitle(string url, string language, SubtitleType type = SubtitleType.VTT)
    {
        Url = url;
        Language = language;
        Type = type;
    }

    public Subtitle(
        string url,
        string language,
        Dictionary<string, string> headers,
        SubtitleType type = SubtitleType.VTT
    )
    {
        Url = url;
        Language = language;
        Headers = headers;
        Type = type;
    }

    /// <inheritdoc />
    public override string ToString() => Language;
}
