using System;
using System.Net.Http;
using Juro.Core;
using Juro.Core.Providers;
using Juro.Core.Utils;

namespace Juro.Clients;

/// <summary>
/// Client for managining all available manga providers.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="MangaClient"/>.
/// </remarks>
public class MangaClient(IHttpClientFactory httpClientFactory) : ClientBase<IMangaProvider>(httpClientFactory)
{

    /// <summary>
    /// Initializes an instance of <see cref="MangaClient"/>.
    /// </summary>
    public MangaClient(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="MangaClient"/>.
    /// </summary>
    public MangaClient()
        : this(Http.ClientProvider) { }
}
