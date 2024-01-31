using System;
using System.Net.Http;
using Juro.Core;
using Juro.Core.Providers;
using Juro.Core.Utils;

namespace Juro.Clients;

/// <summary>
/// Client for managining all available movie providers.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="MovieClient"/>.
/// </remarks>
public class MovieClient(IHttpClientFactory httpClientFactory) : ClientBase<IMovieProvider>(httpClientFactory)
{

    /// <summary>
    /// Initializes an instance of <see cref="MovieClient"/>.
    /// </summary>
    public MovieClient(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="MovieClient"/>.
    /// </summary>
    public MovieClient()
        : this(Http.ClientProvider) { }
}
