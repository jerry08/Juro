using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Juro.Clients;
using Juro.Models.Videos;
using Juro.Utils;

namespace Juro.Extractors
{
    /// <summary>
    /// Extractor for VizCloud.
    /// </summary>
    public class VizCloudExtractor : IVideoExtractor
    {
        private readonly ConsumetClient _consumet;

        private readonly string _consumetAction;

        /// <inheritdoc />
        public string ServerName => "VizCloud";

        /// <summary>
        /// Initializes an instance of <see cref="VizCloudExtractor"/>.
        /// </summary>
        public VizCloudExtractor(
            IHttpClientFactory httpClientFactory,
            string consumetAction)
        {
            _consumetAction = consumetAction;
            _consumet = new ConsumetClient(httpClientFactory);
        }

        /// <summary>
        /// Initializes an instance of <see cref="VizCloudExtractor"/>.
        /// </summary>
        public VizCloudExtractor(
            Func<HttpClient> httpClientProvider,
            string consumetAction)
            : this(new HttpClientFactory(httpClientProvider), consumetAction)
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="VizCloudExtractor"/>.
        /// </summary>
        public VizCloudExtractor(string consumetAction)
            : this(Http.ClientProvider, consumetAction)
        {
        }

        /// <inheritdoc />
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

            return new List<VideoSource>
            {
                new VideoSource
                {
                    VideoUrl = m3u8File,
                    Headers = new Dictionary<string, string>
                    {
                        ["Referer"] = $"https://{new Uri(url).Host}/"
                    },
                    Format = VideoType.M3u8,
                    Resolution = "Multi Quality"
                }
            };
        }
    }
}