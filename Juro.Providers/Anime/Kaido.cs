using System;
using System.Net.Http;
using Juro.Core;
using Juro.Core.Models.Anime;
using Juro.Core.Utils;

namespace Juro.Providers.Anime;

/// <summary>
/// Initializes an instance of <see cref="Kaido"/>.
/// </summary>
public class Kaido(IHttpClientFactory httpClientFactory) : Aniwatch(httpClientFactory)
{
    public override string Key => Name;

    public override string Name => "Kaido";

    public override string BaseUrl => "https://kaido.to";

    public override string AjaxUrl => $"{BaseUrl}/ajax";

    protected override AnimeSites AnimeSite => AnimeSites.Kaido;

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
}
