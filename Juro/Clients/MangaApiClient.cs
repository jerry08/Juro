using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core;
using Juro.Core.Models;
using Juro.Core.Models.Manga;
using Juro.Core.Utils;
using Juro.Core.Utils.Extensions;

namespace Juro.Clients;

public class MangaApiClient(string baseUrl, IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _http = httpClientFactory.CreateClient();
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                static typeInfo =>
                {
                    if (typeInfo.Type == typeof(IMangaInfo))
                        typeInfo.CreateObject = () => new MangaInfo();
                    else if (typeInfo.Type == typeof(IMangaResult))
                        typeInfo.CreateObject = () => new MangaResult();
                    else if (typeInfo.Type == typeof(IMangaChapter))
                        typeInfo.CreateObject = () => new MangaChapter();
                    else if (typeInfo.Type == typeof(IMangaChapterPage))
                        typeInfo.CreateObject = () => new MangaChapterPage();
                },
            },
        },
    };

    public string BaseUrl { get; set; } = baseUrl;

    public string ProviderKey { get; set; } = "Manga";

    /// <summary>
    /// Initializes an instance of <see cref="MangaApiClient"/>.
    /// </summary>
    public MangaApiClient(string baseUrl, Func<HttpClient> httpClientProvider)
        : this(baseUrl, new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="MangaApiClient"/>.
    /// </summary>
    public MangaApiClient(string baseUrl)
        : this(baseUrl, Http.ClientProvider) { }

    public async ValueTask<List<Provider>> GetProvidersAsync(
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/Providers?type={(int)ProviderType.Manga}",
            cancellationToken: cancellationToken
        );

        return JsonSerializer.Deserialize<List<Provider>>(response, _options)!;
    }

    public async ValueTask<IMangaInfo> GetAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/{ProviderKey}/{Uri.EscapeDataString(id)}",
            cancellationToken: cancellationToken
        );

        return JsonSerializer.Deserialize<IMangaInfo>(response, _options)!;
    }

    public async ValueTask<List<IMangaResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/{ProviderKey}/Search?q={Uri.EscapeDataString(query)}",
            cancellationToken: cancellationToken
        );

        return JsonSerializer.Deserialize<List<IMangaResult>>(response, _options) ?? [];
    }

    public async ValueTask<List<IMangaChapterPage>> GetChapterPagesAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _http.ExecuteAsync(
            $"{BaseUrl}/{ProviderKey}/ChapterPages/{Uri.EscapeDataString(id)}",
            cancellationToken: cancellationToken
        );

        return JsonSerializer.Deserialize<List<IMangaChapterPage>>(response, _options) ?? [];
    }
}
