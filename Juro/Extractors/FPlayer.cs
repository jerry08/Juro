using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models.Videos;
using Juro.Utils.Extensions;

namespace Juro.Extractors;

public class FPlayer : IVideoExtractor
{
    private readonly Func<HttpClient> _httpClientProvider;

    public string ServerName => "FPlayer";

    public FPlayer(Func<HttpClient> httpClientProvider)
    {
        _httpClientProvider = httpClientProvider;
    }

    public async ValueTask<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var http = _httpClientProvider();

        var apiLink = url.Replace("/v/", "/api/source/");

        var list = new List<VideoSource>();

        try
        {
            var headers = new Dictionary<string, string>()
            {
                { "Referer", url }
            };

            var json = await http.PostAsync(apiLink, headers, cancellationToken);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var data = JsonNode.Parse(JsonNode.Parse(json)!["data"]!.ToString())!.AsArray();
                for (var i = 0; i < data.Count; i++)
                {
                    list.Add(new()
                    {
                        VideoUrl = data[i]!["file"]!.ToString(),
                        Resolution = data[i]!["label"]!.ToString(),
                        Format = VideoType.Container,
                        FileType = data[i]!["type"]!.ToString(),
                    });
                }

                return list;
            }
        }
        catch
        {
            // Ignore
        }

        return list;
    }
}