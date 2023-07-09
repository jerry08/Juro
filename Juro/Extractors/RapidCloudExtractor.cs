using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models;
using Juro.Models.Videos;
using Juro.Utils;
using Juro.Utils.Extensions;

namespace Juro.Extractors
{
    /// <summary>
    /// Extractor for RapidCloud.
    /// </summary>
    public class RapidCloudExtractor : IVideoExtractor
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly string _fallbackKey = "c1d17096f2ca11b7";
        private readonly string _host = "https://rapid-cloud.co";

        /// <inheritdoc />
        public string ServerName => "RapidCloud";

        /// <summary>
        /// Initializes an instance of <see cref="RapidCloudExtractor"/>.
        /// </summary>
        public RapidCloudExtractor(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Initializes an instance of <see cref="RapidCloudExtractor"/>.
        /// </summary>
        public RapidCloudExtractor(Func<HttpClient> httpClientProvider)
            : this(new HttpClientFactory(httpClientProvider))
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="RapidCloudExtractor"/>.
        /// </summary>
        public RapidCloudExtractor() : this(Http.ClientProvider)
        {
        }

        /// <inheritdoc />
        public async ValueTask<List<VideoSource>> ExtractAsync(
            string url,
            CancellationToken cancellationToken = default)
        {
            var http = _httpClientFactory.CreateClient();

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
            {
                decryptKey = _fallbackKey;
            }

            var data = JsonNode.Parse(response);

            var sources = data?["sources"]?.ToString();
            if (string.IsNullOrWhiteSpace(sources))
            {
                return new List<VideoSource>();
            }

            var isEncrypted = (bool)data!["encrypted"]!;
            if (isEncrypted)
            {
                try
                {
                    sources = AesHelper.Decrypt(sources!, decryptKey);
                }
                catch
                {
                    return new List<VideoSource>();
                }
            }

            var subtitles = new List<Subtitle>();

            var tracksStr = data["tracks"]?.ToString();
            if (!string.IsNullOrWhiteSpace(tracksStr))
            {
                foreach (var subtitle in JsonNode.Parse(tracksStr!)!.AsArray())
                {
                    var kind = subtitle!["kind"]?.ToString();
                    var label = subtitle["label"]?.ToString();
                    var file = subtitle["file"]?.ToString();

                    if (kind == "captions"
                        && !string.IsNullOrEmpty(label)
                        && !string.IsNullOrEmpty(file))
                    {
                        subtitles.Add(new Subtitle(file!, label!));
                    }
                }
            }

            var m3u8File = JsonNode.Parse(sources!)![0]!["file"]!.ToString();

            return new List<VideoSource>
            {
                new()
                {
                    VideoUrl = m3u8File,
                    Headers = headers,
                    Format = VideoType.M3u8,
                    Resolution = "Multi Quality",
                    Subtitles = subtitles
                }
            };
        }
    }
}