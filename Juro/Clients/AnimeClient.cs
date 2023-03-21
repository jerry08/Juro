using System.Net.Http;
using Juro.Providers.Anime;
using Juro.Utils;

namespace Juro.Clients;

public class AnimeClient
{
    public Gogoanime Gogoanime { get; }

    public Zoro Zoro { get; }

    public AnimePahe AnimePahe { get; }

    public AnimeClient(HttpClient httpClient)
    {
        Gogoanime = new(httpClient);
        Zoro = new(httpClient);
        AnimePahe = new(httpClient);
    }

    /// <summary>
    /// Initializes an instance of <see cref="MangaClient"/>.
    /// </summary>
    public AnimeClient() : this(Http.Client)
    {
    }
}