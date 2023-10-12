using System;
using System.Net.Http;
using Juro.Core;
using Juro.Core.Providers;
using Juro.Core.Utils;

namespace Juro.Clients;

/// <summary>
/// Client for managining all available anime providers.
/// </summary>
public class AnimeClient : ClientBase<IAnimeProvider>
{
    /// <summary>
    /// Initializes an instance of <see cref="AnimeClient"/>.
    /// </summary>
    public AnimeClient(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory)
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="AnimeClient"/>.
    /// </summary>
    public AnimeClient(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="AnimeClient"/>.
    /// </summary>
    public AnimeClient()
        : this(Http.ClientProvider) { }
}
