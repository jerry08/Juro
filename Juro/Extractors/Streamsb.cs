﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models.Videos;
using Juro.Utils;
using Juro.Utils.Extensions;
using Newtonsoft.Json.Linq;

namespace Juro.Extractors;

public class StreamSB : IVideoExtractor
{
    private readonly Func<HttpClient> _httpClientProvider;

    private readonly string _alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public string ServerName => "StreamSB";

    public StreamSB(Func<HttpClient> httpClientProvider)
    {
        _httpClientProvider = httpClientProvider;
    }

    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var http = _httpClientProvider();

        var id = url.FindBetween("/e/", ".html");
        if (string.IsNullOrWhiteSpace(id))
            id = url.Split(new[] { "/e/" }, StringSplitOptions.None)[1];

        //var source = await http.ExecuteAsync(
        //    "https://raw.githubusercontent.com/jerry08/anistream-extras/main/streamsb.txt",
        //    cancellationToken
        //);

        var jsonLink = $"https://sbani.pro/375664356a494546326c4b797c7c6e756577776778623171737/{Encode(id)}";

        var headers = new Dictionary<string, string>()
        {
            //{ "watchsb", "streamsb" },
            { "watchsb", "sbstream" },
            { "User-Agent", Http.ChromeUserAgent() },
            { "Referer", url },
        };

        var response = await http.ExecuteAsync(jsonLink, headers, cancellationToken);

        var data = JObject.Parse(response);
        var masterUrl = data["stream_data"]?["file"]?.ToString().Trim('"')!;

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

    private string Encode(string id)
    {
        id = $"{MakeId(12)}||{id}||{MakeId(12)}||streamsb";

        var output = "";
        var arr = id.ToArray();

        for (var i = 0; i < id.Length; i++)
        {
            output += Convert.ToString(Convert.ToInt32(((int)arr[i]).ToString(), 10), 16);
        }

        return output;
    }

    private string MakeId(int length)
    {
        var output = "";

        for (var i = 0; i < length; i++)
        {
            output += _alphabet[(int)Math.Floor(new Random().NextDouble() * _alphabet.Length)];
        }

        return output;
    }
}