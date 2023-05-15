using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Juro.Clients;
using Juro.Models.Videos;

namespace Juro.Extractors;

public class VizCloudExtractor : IVideoExtractor
{
    private readonly ConsumetClient _consumet;

    private readonly string _consumetAction;

    public string ServerName => "VizCloud";

    public VizCloudExtractor(Func<HttpClient> httpClientProvider, string consumetAction)
    {
        _consumetAction = consumetAction;
        _consumet = new(httpClientProvider);
    }

    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default!)
    {
        var vidId = new Stack<string>(url.Split('/')).Pop()?.Split('?')[0]!;

        var playlistUrl = await _consumet.NineAnime.ExecuteActionAsync(
            vidId,
            _consumetAction,
            cancellationToken
        );

        var m3u8File = JsonNode.Parse(playlistUrl)!["media"]!["sources"]![0]!["file"]!.ToString();

        return new()
        {
            new()
            {
                VideoUrl = m3u8File,
                Headers = new()
                {
                    ["Referer"] = $"https://{new Uri(url).Host}/"
                },
                Format = VideoType.M3u8,
                Resolution = "Multi Quality",
            }
        };
    }
}