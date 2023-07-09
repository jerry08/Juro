using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models.Videos;
using Juro.Utils;
using Juro.Utils.Extensions;

namespace Juro.Extractors
{
    /// <summary>
    /// Extractor for Dood.
    /// </summary>
    public class DoodExtractor : IVideoExtractor
    {
        private readonly IHttpClientFactory _httpClientFactory;

        /// <inheritdoc />
        public string ServerName => "Dood";

        /// <summary>
        /// Initializes an instance of <see cref="DoodExtractor"/>.
        /// </summary>
        public DoodExtractor(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Initializes an instance of <see cref="DoodExtractor"/>.
        /// </summary>
        public DoodExtractor(Func<HttpClient> httpClientProvider)
            : this(new HttpClientFactory(httpClientProvider))
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="DoodExtractor"/>.
        /// </summary>
        public DoodExtractor() : this(Http.ClientProvider)
        {
        }

        /// <inheritdoc />
        public async ValueTask<List<VideoSource>> ExtractAsync(
            string url,
            CancellationToken cancellationToken = default)
        {
            var http = _httpClientFactory.CreateClient();

            var list = new List<VideoSource>();

            try
            {
                var response = await http.ExecuteAsync(
                    url,
                    new Dictionary<string, string>()
                    {
                        ["User-Agent"] = "Juro"
                    },
                    cancellationToken
                );

                if (!response.Contains("'/pass_md5/"))
                {
                    return new List<VideoSource>();
                }

                var doodTld = url.SubstringAfter("https://dood.").SubstringBefore("/");
                var md5 = response.SubstringAfter("'/pass_md5/").SubstringBefore("',");
                var token = md5.Split(new[] { "/" }, StringSplitOptions.None).LastOrDefault();
                var randomString = RandomString();
                var expiry = DateTime.Now.CurrentTimeMillis();

                var videoUrlStart = await http.ExecuteAsync(
                    $"https://dood.{doodTld}/pass_md5/{md5}",
                    new Dictionary<string, string>()
                    {
                        ["Referer"] = url,
                        ["User-Agent"] = "Juro"
                    },
                    cancellationToken
                );

                var videoUrl = $"{videoUrlStart}{randomString}?token={token}&expiry={expiry}";

                list.Add(new VideoSource
                {
                    Format = VideoType.Container,
                    VideoUrl = videoUrl,
                    Resolution = "Default Quality",
                    Headers = new Dictionary<string, string>
                    {
                        ["User-Agent"] = "Juro",
                        ["Referer"] = $"https://dood.{doodTld}"
                    }
                });
            }
            catch
            {
                // Ignore
            }

            return list;
        }

        private static Random random = new();

        private static string RandomString(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}