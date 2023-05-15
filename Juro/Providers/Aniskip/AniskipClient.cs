using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Juro.Utils;
using Juro.Utils.Extensions;

namespace Juro.Providers.Aniskip;

/// <summary>
/// Client for interacting with aniskip api.
/// </summary>
public class AniskipClient
{
    private readonly HttpClient _http;

    /// <summary>
    /// Initializes an instance of <see cref="AniskipClient"/>.
    /// </summary>
    public AniskipClient(Func<HttpClient> httpClientProvider)
    {
        _http = httpClientProvider();
    }

    /// <summary>
    /// Initializes an instance of <see cref="AniskipClient"/>.
    /// </summary>
    public AniskipClient() : this(Http.ClientProvider)
    {
    }

    /// <summary>
    /// Gets the skip times associated with the episode.
    /// </summary>
    public async ValueTask<List<Stamp>?> GetAsync(
        int malId, int episodeNumber, long episodeLength,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://api.aniskip.com/v2/skip-times/{malId}/{episodeNumber}?types[]=ed&types[]=mixed-ed&types[]=mixed-op&types[]=op&types[]=recap&episodeLength={episodeLength}";

        var response = await _http.ExecuteAsync(url, cancellationToken);
        if (response is null)
            return null;

        var result = JsonSerializer.Deserialize<AniSkipResponse>(
            response,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        );

        return result?.IsFound == true ? result.Results : null;
    }
}