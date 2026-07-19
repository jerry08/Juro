using System;
using System.Net.Http;
using Juro.Core;
using Juro.Core.Models.Anime;
using Juro.Core.Utils;

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with Anikoto (anikototv.to), an Anikoto-theme site.
/// Mirrors: anikoto.bz, anikoto.cz, anikoto.me, anikoto.net, anikototv.se.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="Anikoto"/>.
/// </remarks>
public class Anikoto(IHttpClientFactory httpClientFactory) : AnikotoThemeProvider(httpClientFactory)
{
    public override string Name => "Anikoto";

    public override string BaseUrl => "https://anikototv.to";

    protected override AnimeSites Site => AnimeSites.Anikoto;

    /// <summary>
    /// Initializes an instance of <see cref="Anikoto"/>.
    /// </summary>
    public Anikoto(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="Anikoto"/>.
    /// </summary>
    public Anikoto()
        : this(Http.ClientProvider) { }
}
