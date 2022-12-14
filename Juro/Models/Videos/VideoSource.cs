using System.Collections.Generic;

namespace Juro.Models.Videos;

public class VideoSource
{
    /// <summary>
    /// Will represent quality to user in form of `$"{quality}p"` (1080p).
    /// If quality is null, shows "Unknown Quality".
    /// If isM3U8 is true, shows "Multi Quality"
    /// </summary>
    public string Resolution { get; set; } = default!;

    /// <summary>
    /// The direct url to the Video.
    /// Supports mp4, mkv and m3u8 for now, afaik
    /// </summary>
    public string VideoUrl { get; set; } = default!;

    /// <summary>
    /// No need to set it on M3U8 links
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// The direct url to the Video
    /// Supports mp4, mkv, dash &#38; m3u8, afaik
    /// </summary>
    public string FileType { get; set; } = default!;

    /// <summary>
    /// If not a "CONTAINER" format, the app show video as a "Multi Quality" Link
    /// "CONTAINER" formats are Mp4 &#38; Mkv
    /// </summary>
    public VideoType Format { get; set; }

    /// <summary>
    /// The direct url to the Video
    /// Supports mp4, mkv, dash &#38; m3u8, afaik
    /// </summary>
    //public FileUrl Url { get; set; } = default!;

    /// <summary>
    /// In case, you want to show some extra notes to the User
    /// Ex: "Backup" which could be used if the site provides some
    /// </summary>
    public string ExtraNote { get; set; } = default!;

    public Dictionary<string, string> Headers { get; set; } = new();

    public List<Subtitle> Subtitles { get; set; } = new();
}