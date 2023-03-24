using System;
using System.Net.Http;
using Juro.Providers.Anime;
using Juro.Utils;

namespace Juro.Clients;

public class AnimeClient
{
    public Gogoanime Gogoanime { get; }

    public Zoro Zoro { get; }

    public AnimePahe AnimePahe { get; }

    /// <summary>
    /// Initializes an instance of <see cref="AnimeClient"/>.
    /// </summary>
    public AnimeClient(Func<HttpClient> httpClientProvider)
    {
        Gogoanime = new(httpClientProvider);
        Zoro = new(httpClientProvider);
        AnimePahe = new(httpClientProvider);
    }

    /// <summary>
    /// Initializes an instance of <see cref="AnimeClient"/>.
    /// </summary>
    public AnimeClient() : this(Http.ClientProvider)
    {
    }
}