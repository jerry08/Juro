using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Juro.Providers.Manga;
using Juro.Utils;

namespace Juro.Clients;

/// <summary>
/// Client for managining all available manga providers.
/// </summary>
public class MangaClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes an instance of <see cref="MangaClient"/>.
    /// </summary>
    public MangaClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Initializes an instance of <see cref="MangaClient"/>.
    /// </summary>
    public MangaClient(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider))
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="MangaClient"/>.
    /// </summary>
    public MangaClient() : this(Http.ClientProvider)
    {
    }

    public IList<IMangaProvider> GetAllProviders() => GetProviders();

    public IList<IMangaProvider> GetProviders(string? language = null)
        => Assembly.GetExecutingAssembly().GetTypes()
            .Where(x => x.GetInterfaces().Contains(typeof(IMangaProvider))
                && x.GetConstructor(Type.EmptyTypes) != null)
            .Select(x => (IMangaProvider)Activator.CreateInstance(x, new object[] { _httpClientFactory })!)
            .Where(x => string.IsNullOrEmpty(language)
                || x.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
            .ToList();
}