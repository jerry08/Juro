using System;
using System.Net.Http;
using Juro.Providers.Manga;
using Juro.Utils;

namespace Juro.Clients;

public class MangaClient
{
    public MangaKakalot MangaKakalot { get; }

    public Mangadex Mangadex { get; }

    public MangaPill MangaPill { get; }

    public MangaKatana MangaKatana { get; }

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