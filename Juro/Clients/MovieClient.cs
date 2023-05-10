using System;
using System.Net.Http;
using Juro.Providers.Movie;
using Juro.Utils;

namespace Juro.Clients;

/// <summary>
/// Client for interacting with various movie providers.
/// </summary>
public class MovieClient
{
    /// <summary>
    /// Operations related to FlixHQ.
    /// </summary>
    public FlixHQ FlixHQ { get; }

    /// <summary>
    /// Initializes an instance of <see cref="MovieClient"/>.
    /// </summary>
    public MovieClient(Func<HttpClient> httpClientProvider)
    {
        FlixHQ = new(httpClientProvider);
    }

    /// <summary>
    /// Initializes an instance of <see cref="MovieClient"/>.
    /// </summary>
    public MovieClient() : this(Http.ClientProvider)
    {
    }
}