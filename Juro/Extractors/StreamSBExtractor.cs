using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models.Videos;
using Juro.Utils;
using Juro.Utils.Extensions;

namespace Juro.Extractors
{
    /// <summary>
    /// Extractor for StreamSB.
    /// </summary>
    public class StreamSBExtractor : IVideoExtractor
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly char[] hexArray = "0123456789ABCDEF".ToCharArray();

        /// <inheritdoc />
        public string ServerName => "StreamSB";

        /// <summary>
        /// Initializes an instance of <see cref="StreamSBExtractor"/>.
        /// </summary>
        public StreamSBExtractor(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Initializes an instance of <see cref="StreamSBExtractor"/>.
        /// </summary>
        public StreamSBExtractor(Func<HttpClient> httpClientProvider)
            : this(new HttpClientFactory(httpClientProvider))
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="StreamSBExtractor"/>.
        /// </summary>
        public StreamSBExtractor() : this(Http.ClientProvider)
        {
        }

        /// <inheritdoc />
        public async ValueTask<List<VideoSource>> ExtractAsync(
            string url,
            CancellationToken cancellationToken = default)
        {
            var http = _httpClientFactory.CreateClient();

            var id = url.FindBetween("/e/", ".html");
            if (string.IsNullOrWhiteSpace(id))
            {
                id = url.Split(new[] { "/e/" }, StringSplitOptions.None)[1];
            }

            var bytes = Encoding.ASCII.GetBytes($"||{id}||||streamsb");
            var bytesToHex = BytesToHex(bytes);

            var source = await http.ExecuteAsync(
                "https://raw.githubusercontent.com/jerry08/anistream-extras/main/streamsb.txt",
                cancellationToken
            );

            var jsonLink = $"{source.Trim()}/{bytesToHex}/";

            var headers = new Dictionary<string, string>()
            {
                //{ "watchsb", "streamsb" },
                { "watchsb", "sbstream" },
                { "User-Agent", Http.ChromeUserAgent() },
                { "Referer", url }
            };

            var response = await http.ExecuteAsync(jsonLink, headers, cancellationToken);

            var data = JsonNode.Parse(response)!;
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

        private string BytesToHex(byte[] bytes)
        {
            var hexChars = new char[bytes.Length * 2];
            for (var j = 0; j < bytes.Length; j++)
            {
                var v = bytes[j] & 0xFF;

                hexChars[j * 2] = hexArray[v >> 4];
                hexChars[j * 2 + 1] = hexArray[v & 0x0F];
            }

            return new string(hexChars);
        }
    }
}