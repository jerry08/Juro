using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Juro.Models;
using Juro.Models.Videos;
using Juro.Utils;
using Juro.Utils.Extensions;
using Juro.Extractors.Decryptors;

namespace Juro.Extractors;

internal class VidCloud : VideoExtractor
{
    private readonly string _host = "https://dokicloud.one";
    private readonly string _host2 = "https://rabbitstream.net";
    private readonly bool _isAlternative;

    public override string ServerName => "VidCloud";

    public VidCloud(HttpClient http, bool isAlternative = false) : base(http)
    {
        _isAlternative = isAlternative;
    }

    public override async Task<List<VideoSource>> ExtractAsync(
        string videoUrl,
        CancellationToken cancellationToken = default!)
    {
        var id = new Stack<string>(videoUrl.Split('/')).Pop()?.Split('?')[0];
        var headers = new Dictionary<string, string>()
        {
            { "X-Requested-With", "XMLHttpRequest" },
            { "Referer", videoUrl },
            { "User-Agent", Http.RandomUserAgent() }
        };

        var response = await _http.ExecuteAsync($"{(_isAlternative ? _host2 : _host)}/ajax/embed-4/getSources?id={id}", headers, cancellationToken);

        var data = JObject.Parse(response);
        var sourcesJson = data["sources"]!.ToString();

        if (!JsonExtensions.IsValidJson(sourcesJson))
        {
            var key = await _http.ExecuteAsync("https://raw.githubusercontent.com/consumet/rapidclown/rabbitstream/key.txt", cancellationToken);

            var decryptor = new VidCloudDecryptor();
            sourcesJson = decryptor.Decrypt(sourcesJson, key);
        }

        var sources = JArray.Parse(sourcesJson);

        var subtitles = data["tracks"]!
            .Where(x => x["kind"]?.ToString() == "captions")
            .Select(track => new Subtitle()
        {
            Url = track["file"]!.ToString(),
            Language = track["label"]!.ToString()
        }).ToList();

        var list = sources.Select(source => new VideoSource()
        {
            VideoUrl = source["file"]!.ToString(),
            Format = source["file"]!.ToString().Contains(".m3u8")
                ? VideoType.M3u8 : source["type"]!.ToString().ToLower() switch
                {
                    "hls" => VideoType.Hls,
                    _ => VideoType.Container
                },
            Subtitles = subtitles
        }).ToList();

        return list;
    }
}