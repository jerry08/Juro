using System;
using System.Net.Http;
using Juro.Core;
using Juro.Core.Utils;

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with Hentai Stream.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="HentaiStream"/>.
/// </remarks>
public class HentaiStream(IHttpClientFactory httpClientFactory) : HentaiFF(httpClientFactory)
{
    public override string Key => Name;

    public override string Name => "Hentai Stream";

    public override string BaseUrl => "https://hstream.moe";

    /// <summary>
    /// Initializes an instance of <see cref="HentaiStream"/>.
    /// </summary>
    public HentaiStream(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="HentaiStream"/>.
    /// </summary>
    public HentaiStream()
        : this(Http.ClientProvider) { }
}
