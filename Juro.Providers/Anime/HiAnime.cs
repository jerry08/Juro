using System;
using System.Collections.Generic;
using System.Net.Http;
using Juro.Core;
using Juro.Core.Models.Anime;
using Juro.Core.Models.Videos;
using Juro.Core.Utils;
using Juro.Extractors;
using Juro.Providers.Anime.Zoro;

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with HiAnime (formerly Zoro.to / Aniwatch).
/// </summary>
public class HiAnime(IHttpClientFactory httpClientFactory) : ZoroTheme
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public override string Key => Name;
    public override string Name => "HiAnime";
    public override string Language => "en";
    public override string BaseUrl => "https://hianime.to";

    /// <summary>
    /// HiAnime uses /ajax/v2/ for its API endpoints.
    /// </summary>
    protected override string AjaxRoute => "/v2";

    protected override List<string> HosterNames => ["HD-1", "HD-2", "StreamTape", "StreamSB"];

    /// <summary>
    /// Initializes an instance of <see cref="HiAnime"/>.
    /// </summary>
    public HiAnime(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="HiAnime"/>.
    /// </summary>
    public HiAnime()
        : this(Http.ClientProvider) { }

    protected override AnimeSites GetAnimeSite() => AnimeSites.Zoro;

    public override IVideoExtractor? GetVideoExtractor(VideoServer server)
    {
        var serverNameLower = server.Name.ToLower();

        return serverNameLower switch
        {
            var s when s.Contains("hd-1") || s.Contains("vidcloud") => new MegaCloudExtractor(
                _httpClientFactory
            ),
            var s when s.Contains("hd-2") || s.Contains("vidstreaming") => new MegaCloudExtractor(
                _httpClientFactory
            ),
            var s when s.Contains("streamtape") => new StreamTapeExtractor(_httpClientFactory),
            var s when s.Contains("streamsb") => new StreamSBExtractor(_httpClientFactory),
            _ => base.GetVideoExtractor(server),
        };
    }
}
