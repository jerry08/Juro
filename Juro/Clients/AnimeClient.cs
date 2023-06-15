using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Juro.Providers.Anime;
using Juro.Utils;

namespace Juro.Clients;

/// <summary>
/// Client for managining all available anime providers.
/// </summary>
public class AnimeClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes an instance of <see cref="AnimeClient"/>.
    /// </summary>
    public AnimeClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Initializes an instance of <see cref="AnimeClient"/>.
    /// </summary>
    public AnimeClient(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider))
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="AnimeClient"/>.
    /// </summary>
    public AnimeClient() : this(Http.ClientProvider)
    {
    }

    public IList<IAnimeProvider> GetAllProviders() => GetProviders();

    public IList<IAnimeProvider> GetProviders(string? language = null)
        => Assembly.GetExecutingAssembly().GetTypes()
            .Where(x => x.GetInterfaces().Contains(typeof(IAnimeProvider))
                && x.GetConstructor(Type.EmptyTypes) != null)
            .Select(x => (IAnimeProvider)Activator.CreateInstance(x, new object[] { _httpClientFactory })!)
            .Where(x => x.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
            .ToList();
}