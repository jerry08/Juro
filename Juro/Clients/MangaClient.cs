using System.Net.Http;
using Juro.Utils;
using Juro.Providers.Manga;

namespace Juro.Clients;

public class MangaClient
{
    public MangaKakalot MangaKakalot { get; }

    public Mangadex Mangadex { get; }

    public MangaPill MangaPill { get; }

    public MangaClient(HttpClient httpClient)
    {
        MangaKakalot = new(httpClient);
        Mangadex = new(httpClient);
        MangaPill = new(httpClient);
    }

    /// <summary>
    /// Initializes an instance of <see cref="MangaClient"/>.
    /// </summary>
    public MangaClient() : this(Http.Client)
    {
    }
}