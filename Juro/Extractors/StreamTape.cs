using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models.Videos;
using Juro.Utils.Extensions;

namespace Juro.Extractors;

public class StreamTape : IVideoExtractor
{
    private readonly HttpClient _http;

    private readonly Regex _linkRegex = new(@"'robotlink'\)\.innerHTML = '(.+?)'\+ \('(.+?)'\)");

    public virtual string MainUrl => "https://streamtape.com";

    public string ServerName => "StreamTape";

    public StreamTape(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var text = await _http.ExecuteAsync(url, cancellationToken);
        var reg = _linkRegex.Match(text);

        var vidUrl = $"https:{reg.Groups[1]!.Value + reg.Groups[2]!.Value.Substring(3)}";

        var list = new List<VideoSource>
        {
            new()
            {
                Format = VideoType.M3u8,
                VideoUrl = vidUrl,
                Resolution = "Multi Quality",
            }
        };

        return list;
    }
}