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

    public MangaClient(HttpClient httpClient)
    {
        MangaKakalot = new(httpClient);
        Mangadex = new(httpClient);
        MangaPill = new(httpClient);
        MangaKatana = new(httpClient);
    }

    /// <summary>
    /// Initializes an instance of <see cref="MangaClient"/>.
    /// </summary>
    public MangaClient() : this(Http.Client)
    {
    }
}