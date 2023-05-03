using System;
using System.Net.Http;
using Juro.Providers.Anime;
using Juro.Utils;

namespace Juro.Clients;

/// <summary>
/// Client for interacting with various anime providers.
/// </summary>
public class AnimeClient
{
    /// <summary>
    /// Operations related to Gogoanime.
    /// </summary>
    public Gogoanime Gogoanime { get; }

    /// <summary>
    /// Operations related to Zoro.
    /// </summary>
    public Zoro Zoro { get; }

    /// <summary>
    /// Operations related to AnimePahe.
    /// </summary>
    public AnimePahe AnimePahe { get; }

    /// <summary>
    /// Operations related to NineAnime.
    /// </summary>
    public NineAnime NineAnime { get; }

    /// <summary>
    /// Initializes an instance of <see cref="AnimeClient"/>.
    /// </summary>
    public AnimeClient(Func<HttpClient> httpClientProvider)
    {
        Gogoanime = new(httpClientProvider);
        Zoro = new(httpClientProvider);
        AnimePahe = new(httpClientProvider);
        NineAnime = new(httpClientProvider);
    }

    /// <summary>
    /// Initializes an instance of <see cref="AnimeClient"/>.
    /// </summary>
    public AnimeClient() : this(Http.ClientProvider)
    {
    }
}