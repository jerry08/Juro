using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models.Videos;
using Juro.Utils;
using Juro.Utils.Extensions;

namespace Juro.Extractors
{
    /// <summary>
    /// Extractor for Mp4upload.
    /// </summary>
    public class Mp4uploadExtractor : IVideoExtractor
    {
        private readonly IHttpClientFactory _httpClientFactory;

        /// <inheritdoc />
        public string ServerName => "Mp4upload";

        /// <summary>
        /// Initializes an instance of <see cref="Mp4uploadExtractor"/>.
        /// </summary>
        public Mp4uploadExtractor(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Initializes an instance of <see cref="Mp4uploadExtractor"/>.
        /// </summary>
        public Mp4uploadExtractor(Func<HttpClient> httpClientProvider)
            : this(new HttpClientFactory(httpClientProvider))
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="Mp4uploadExtractor"/>.
        /// </summary>
        public Mp4uploadExtractor() : this(Http.ClientProvider)
        {
        }

        /// <inheritdoc />
        public async ValueTask<List<VideoSource>> ExtractAsync(
            string url,
            CancellationToken cancellationToken = default)
        {
            var http = _httpClientFactory.CreateClient();

            var headers = new Dictionary<string, string>()
            {
                ["Referer"] = "https://mp4upload.com/"
            };

            var response = await http.ExecuteAsync(url, headers, cancellationToken);

            var packed = response.SubstringAfter("eval(function(p,a,c,k,e,d)")
                .Split(new[] { "</script>" }, StringSplitOptions.None)[0];

            var unpacked = JsUnpacker.UnpackAndCombine($"eval(function(p,a,c,k,e,d){packed}");

            if (string.IsNullOrEmpty(unpacked))
            {
                return new List<VideoSource>();
            }

            var videoUrl = unpacked.SubstringAfter("player.src(\"")
                .Split(new[] { "\");" }, StringSplitOptions.None)[0];

            return new List<VideoSource>
            {
                new()
                {
                    Format = VideoType.Container,
                    VideoUrl = videoUrl,
                    Resolution = "Default Quality",
                    Headers = headers
                }
            };
        }
    }
}