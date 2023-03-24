using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models.Videos;
using Juro.Utils.Extensions;
using Newtonsoft.Json.Linq;

namespace Juro.Extractors;

public class RapidCloud : IVideoExtractor
{
    private readonly Func<HttpClient> _httpClientProvider;

    public string ServerName => "RapidCloud";

    private readonly string _fallbackKey = "c1d17096f2ca11b7";
    //private readonly string _consumetApi = "https://consumet-api.herokuapp.com";
    private readonly string _consumetApi = "https://api.consumet.org";
    private readonly string _enimeApi = "https://api.enime.moe";
    private readonly string _host = "https://rapid-cloud.co";

    public RapidCloud(Func<HttpClient> httpClientProvider)
    {
        _httpClientProvider = httpClientProvider;
    }

    public async Task<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var http = _httpClientProvider();

        var id = new Stack<string>(url.Split('/')).Pop().Split('?')[0];
        //var sId = await http.ExecuteAsync(consumetApi + "/utils/rapid-cloud", cancellationToken);
        var sId = await http.ExecuteAsync(
            $"{_enimeApi}/tool/rapid-cloud/server-id",
            new Dictionary<string, string>()
            {
                { "User-Agent", "Saikou" }
            },
            cancellationToken
        );

        if (string.IsNullOrEmpty(sId))
        {
            sId = await http.ExecuteAsync(
                $"{_enimeApi}/tool/rapid-cloud/server-id",
                cancellationToken
            );
        }

        var headers = new Dictionary<string, string>()
        {
            { "X-Requested-With", "XMLHttpRequest" }
        };

        var res = await http.ExecuteAsync(
            $"{_host}/ajax/embed-6/getSources?id={id}&sId={sId}",
            headers,
            cancellationToken
        );

        var decryptKey = await http.ExecuteAsync(
            "https://raw.githubusercontent.com/consumet/rapidclown/main/key.txt",
            cancellationToken
        );

        if (string.IsNullOrEmpty(decryptKey))
            decryptKey = _fallbackKey;

        var jObj = JObject.Parse(res);

        var sources = jObj["sources"]?.ToString()!;

        var isEncrypted = (bool)jObj["encrypted"]!;
        if (isEncrypted)
            sources = new RapidCloudDecryptor().Decrypt(sources, decryptKey);

        var m3u8File = JArray.Parse(sources)[0]["file"]?.ToString()!;

        return new List<VideoSource>
        {
            new()
            {
                VideoUrl = m3u8File,
                Headers = headers,
                Format = VideoType.M3u8,
                Resolution = "Multi Quality"
            }
        };
    }
}