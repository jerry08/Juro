using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core;
using Juro.Core.Models;
using Juro.Core.Models.Anime;
using Juro.Core.Models.Videos;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;

namespace Juro.Clients;

public class AnimeApiClient(string baseUrl, IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _http = httpClientFactory.CreateClient();
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    public string BaseUrl { get; set; } = baseUrl;

    public string ProviderKey { get; set; } = "Gogoanime";

    /// <summary>
    /// Initializes an instance of <see cref="AnimeApiClient"/>.
    /// </summary>
    public AnimeApiClient(string baseUrl, Func<HttpClient> httpClientProvider)
        : this(baseUrl, new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="AnimeApiClient"/>.
    /// </summary>
    public AnimeApiClient(string baseUrl)
        : this(baseUrl, Http.ClientProvider) { }

    public async ValueTask<List<Provider>> GetProvidersAsync(
        ProviderType type,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/Providers?type={type}",
            cancellationToken: cancellationToken
        );

        return JsonSerializer.Deserialize<List<Provider>>(response, _options)!;
    }

    public async ValueTask<Provider> GetDefaultProviderAsync(
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/Providers",
            cancellationToken: cancellationToken
        );

        return JsonSerializer.Deserialize<Provider>(response, _options)!;
    }

    public async ValueTask<IAnimeInfo> GetAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/{ProviderKey}/{Uri.EscapeDataString(id)}",
            cancellationToken: cancellationToken
        );

        return JsonSerializer.Deserialize<AnimeInfo>(response, _options)!;
    }

    public async ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/{ProviderKey}/Search?query={Uri.EscapeDataString(query)}",
            cancellationToken: cancellationToken
        );

        return JsonSerializer
                .Deserialize<List<AnimeInfo>>(response, _options)
                ?.Cast<IAnimeInfo>()
                .ToList() ?? [];
    }

    public async ValueTask<List<Episode>> GetEpisodesAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/{ProviderKey}/Episodes/{Uri.EscapeDataString(id)}",
            cancellationToken: cancellationToken
        );

        return JsonSerializer.Deserialize<List<Episode>>(response, _options) ?? [];
    }

    public async ValueTask<List<VideoServer>> GetVideoServersAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/{ProviderKey}/VideoServers/{Uri.EscapeDataString(id)}",
            cancellationToken: cancellationToken
        );

        return JsonSerializer.Deserialize<List<VideoServer>>(response, _options) ?? [];
    }

    public async ValueTask<List<VideoSource>> GetVideosAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/{ProviderKey}/Videos?q={Uri.EscapeDataString(query)}",
            cancellationToken: cancellationToken
        );

        return JsonSerializer.Deserialize<List<VideoSource>>(response, _options) ?? [];
    }
}
