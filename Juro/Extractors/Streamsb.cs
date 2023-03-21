using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models;
using Juro.Models.Videos;
using Juro.Utils;
using Juro.Utils.Extensions;
using Newtonsoft.Json.Linq;

namespace Juro.Extractors;

public class StreamSB : IVideoExtractor
{
    private readonly HttpClient _http;

    private readonly char[] hexArray = "0123456789ABCDEF".ToCharArray();

    public string ServerName => "StreamSB";

    public StreamSB(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var id = url.FindBetween("/e/", ".html");
        if (string.IsNullOrEmpty(id))
            id = url.Split(new[] { "/e/" }, StringSplitOptions.None)[1];

        var bytes = Encoding.ASCII.GetBytes($"||{id}||||streamsb");
        var bytesToHex = BytesToHex(bytes);

        var source = await _http.ExecuteAsync(
            "https://raw.githubusercontent.com/jerry08/anistream-extras/main/streamsb.txt",
            cancellationToken
        );

        var jsonLink = $"{source.Trim()}/{bytesToHex}/";

        var headers = new Dictionary<string, string>()
        {
            //{ "watchsb", "streamsb" },
            { "watchsb", "sbstream" },
            { "User-Agent", Http.ChromeUserAgent() },
            { "Referer", url },
        };

        var json = await _http.ExecuteAsync(jsonLink, headers, cancellationToken);

        var jObj = JObject.Parse(json);
        var masterUrl = jObj["stream_data"]?["file"]?.ToString().Trim('"')!;

        return new List<VideoSource>
        {
            new()
            {
                Format = VideoType.M3u8,
                VideoUrl = masterUrl,
                Headers = headers,
                Resolution = "Multi Quality"
            }
        };
    }

    private string BytesToHex(byte[] bytes)
    {
        var hexChars = new char[bytes.Length * 2];
        for (var j = 0; j < bytes.Length; j++)
        {
            var v = bytes[j] & 0xFF;

            hexChars[j * 2] = hexArray[v >> 4];
            hexChars[(j * 2) + 1] = hexArray[v & 0x0F];
        }

        return new string(hexChars);
    }
}