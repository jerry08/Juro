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
/// Client for interacting with Kaido.to.
/// </summary>
public class Kaido(IHttpClientFactory httpClientFactory) : ZoroTheme
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public override string Key => Name;
    public override string Name => "Kaido";
    public override string Language => "en";
    public override string BaseUrl => "https://kaido.to";

    protected override List<string> HosterNames => ["Vidstreaming", "VidCloud", "StreamTape"];

    /// <summary>
    /// Initializes an instance of <see cref="Kaido"/>.
    /// </summary>
    public Kaido(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="Kaido"/>.
    /// </summary>
    public Kaido()
        : this(Http.ClientProvider) { }

    protected override AnimeSites GetAnimeSite() => AnimeSites.Kaido;

    public override IVideoExtractor? GetVideoExtractor(VideoServer server)
    {
        var serverNameLower = server.Name.ToLower();

        return serverNameLower switch
        {
            var s when s.Contains("vidcloud") || s.Contains("vidstreaming") =>
                new MegaCloudExtractor(_httpClientFactory),
            var s when s.Contains("streamtape") => new StreamTapeExtractor(_httpClientFactory),
            _ => base.GetVideoExtractor(server),
        };
    }
}
