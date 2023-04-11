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

        var response = await http.ExecuteAsync(
            $"{_host}/ajax/embed-6/getSources?id={id}",
            cancellationToken
        );

        var headers = new Dictionary<string, string>()
        {
            { "X-Requested-With", "XMLHttpRequest" }
        };

        var decryptKey = await http.ExecuteAsync(
            "https://raw.githubusercontent.com/enimax-anime/key/e6/key.txt",
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(decryptKey))
            decryptKey = _fallbackKey;

        var jObj = JObject.Parse(response);

        var sources = jObj["sources"]?.ToString();
        if (string.IsNullOrWhiteSpace(sources))
            return new();

        var isEncrypted = (bool)jObj["encrypted"]!;
        if (isEncrypted)
        {
            try
            {
                sources = new RapidCloudDecryptor().Decrypt(sources!, decryptKey);
            }
            catch
            {
                return new();
            }
        }

        var m3u8File = JArray.Parse(sources!)[0]["file"]?.ToString()!;

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