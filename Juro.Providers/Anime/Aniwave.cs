using System;
using System.Net.Http;
using Juro.Core;
using Juro.Core.Models.Anime;
using Juro.Core.Utils;

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with Aniwave (animewave.to), an Anikoto-theme site.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="Aniwave"/>.
/// </remarks>
public class Aniwave(IHttpClientFactory httpClientFactory) : AnikotoThemeProvider(httpClientFactory)
{
    public override string Name => "Aniwave";

    public override string BaseUrl => "https://animewave.to";

    protected override AnimeSites Site => AnimeSites.Aniwave;

    /// <summary>
    /// Initializes an instance of <see cref="Aniwave"/>.
    /// </summary>
    public Aniwave(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="Aniwave"/>.
    /// </summary>
    public Aniwave()
        : this(Http.ClientProvider) { }
}
