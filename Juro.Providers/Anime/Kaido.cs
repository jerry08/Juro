using System;
using System.Net.Http;
using Juro.Core;
using Juro.Core.Models.Anime;
using Juro.Core.Utils;

namespace Juro.Providers.Anime;

public class Kaido : Aniwatch
{
    public override string Name => "Kaido";

    public override string BaseUrl => "https://kaido.to";

    public override string AjaxUrl => $"{BaseUrl}/ajax";

    protected override AnimeSites AnimeSite => AnimeSites.Kaido;

    /// <summary>
    /// Initializes an instance of <see cref="Kaido"/>.
    /// </summary>
    public Kaido(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="Kaido"/>.
    /// </summary>
    public Kaido(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider))
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="Kaido"/>.
    /// </summary>
    public Kaido() : this(Http.ClientProvider)
    {
    }
}