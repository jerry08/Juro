using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Utils;
using Juro.Utils.Extensions;
using Newtonsoft.Json.Linq;

namespace Juro.Providers.Consumet;

/// <summary>
/// Client for interacting with consumet 9anime.
/// </summary>
public class NineAnime
{
    private readonly Func<HttpClient> _httpClientProvider;

    /// <summary>
    /// Initializes an instance of <see cref="NineAnime"/>.
    /// </summary>
    public NineAnime(Func<HttpClient> httpClientProvider)
    {
        _httpClientProvider = httpClientProvider;
    }

    /// <summary>
    /// Initializes an instance of <see cref="ConsumetProvider"/>.
    /// </summary>
    public NineAnime() : this(Http.ClientProvider)
    {
    }

    public async Task<string> ExecuteActionAsync(
        string query,
        string action,
        CancellationToken cancellationToken = default)
    {
        var http = _httpClientProvider();

        var response = await http.ExecuteAsync(
            $"https://api.consumet.org/anime/9anime/helper?query={query}&action={action}",
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(response))
            return string.Empty;

        var data = JObject.Parse(response)!;

        if (action is "vizcloud" or "mcloud")
            return data["data"]!.ToString();

        var vrf = data["url"]?.ToString();

        if (!string.IsNullOrWhiteSpace(vrf))
            return vrf!;

        return string.Empty;
    }
}