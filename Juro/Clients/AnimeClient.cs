using System;
using System.Net.Http;
using Juro.Core;
using Juro.Core.Providers;
using Juro.Core.Utils;

namespace Juro.Clients;

/// <summary>
/// Client for managining all available anime providers.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="AnimeClient"/>.
/// </remarks>
public class AnimeClient(IHttpClientFactory httpClientFactory)
    : ClientBase<IAnimeProvider>(httpClientFactory)
{
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
