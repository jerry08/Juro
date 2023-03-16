using System.Net.Http;
using Juro.Providers.Movies;
using Juro.Utils;

namespace Juro.Clients;

public class MovieClient
{
    public FlixHQ FlixHQ { get; }

    public MovieClient(HttpClient httpClient)
    {
        FlixHQ = new(httpClient);
    }

    /// <summary>
    /// Initializes an instance of <see cref="MovieClient"/>.
    /// </summary>
    public MovieClient() : this(Http.Client)
    {
    }
}