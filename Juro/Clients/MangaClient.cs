using System;
using System.Net.Http;
using Juro.Providers.Manga;
using Juro.Utils;

namespace Juro.Clients;

/// <summary>
/// Client for interacting with various manga providers.
/// </summary>
public class MangaClient
{
    /// <summary>
    /// Operations related to MangaKakalot.
    /// </summary>
    public MangaKakalot MangaKakalot { get; }

    /// <summary>
    /// Operations related to Mangadex.
    /// </summary>
    public Mangadex Mangadex { get; }

    /// <summary>
    /// Operations related to MangaPill.
    /// </summary>
    public MangaPill MangaPill { get; }

    /// <summary>
    /// Operations related to MangaKatana.
    /// </summary>
    public MangaKatana MangaKatana { get; }

    /// <summary>
    /// Initializes an instance of <see cref="MangaClient"/>.
    /// </summary>
    public MangaClient(Func<HttpClient> httpClientProvider)
    {
        MangaKakalot = new(httpClientProvider);
        Mangadex = new(httpClientProvider);
        MangaPill = new(httpClientProvider);
        MangaKatana = new(httpClientProvider);
    }

    /// <summary>
    /// Initializes an instance of <see cref="MangaClient"/>.
    /// </summary>
    public MangaClient() : this(Http.ClientProvider)
    {
    }
}