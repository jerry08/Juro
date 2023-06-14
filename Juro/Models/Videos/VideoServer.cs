namespace Juro.Models;

/// <summary>
/// A simple class containing name, link and extraData(in case you want to give some to it)
/// of the embed which shows the video present on the site.
/// </summary>
public class VideoServer
{
    public string Name { get; set; } = default!;

    public FileUrl Embed { get; set; } = default!;

    public VideoServer()
    {
    }

    public VideoServer(string name, FileUrl embed)
    {
        Name = name;
        Embed = embed;
    }

    /// <inheritdoc />
    public override string ToString() => Name;
}