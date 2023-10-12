using System;
using System.Net.Http;
using Juro.Core;
using Juro.Core.Providers;
using Juro.Core.Utils;

namespace Juro.Clients;

/// <summary>
/// Client for managining all available manga providers.
/// </summary>
public class MangaClient : ClientBase<IMangaProvider>
{
    /// <summary>
    /// Initializes an instance of <see cref="MangaClient"/>.
    /// </summary>
    public MangaClient(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

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
