using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Extractors.Decryptors;
using Juro.Models;
using Juro.Models.Videos;
using Juro.Utils;
using Juro.Utils.Extensions;
using Newtonsoft.Json.Linq;

namespace Juro.Extractors;

public class VidCloud : IVideoExtractor
{
    private readonly HttpClient _http;

    private readonly string _host = "https://dokicloud.one";
    private readonly string _host2 = "https://rabbitstream.net";
    private readonly bool _isAlternative;

    public string ServerName => "VidCloud";

    public VidCloud(HttpClient http, bool isAlternative = false)
    {
        _http = http;
        _isAlternative = isAlternative;
    }

    public async Task<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default!)
    {
        var id = new Stack<string>(url.Split('/')).Pop()?.Split('?')[0];
        var headers = new Dictionary<string, string>()
        {
            { "X-Requested-With", "XMLHttpRequest" },
            { "Referer", url },
            { "User-Agent", Http.RandomUserAgent() }
        };

        var response = await _http.ExecuteAsync(
            $"{(_isAlternative ? _host2 : _host)}/ajax/embed-4/getSources?id={id}",
            headers,
            cancellationToken
        );

        var data = JObject.Parse(response);
        var sourcesJson = data["sources"]!.ToString();

        if (!JsonExtensions.IsValidJson(sourcesJson))
        {
            //var key = await _http.ExecuteAsync("https://raw.githubusercontent.com/consumet/rapidclown/rabbitstream/key.txt", cancellationToken);
            var key = await _http.ExecuteAsync(
                "https://raw.githubusercontent.com/enimax-anime/key/e4/key.txt",
                cancellationToken
            );

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