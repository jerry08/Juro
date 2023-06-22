using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models;
using Juro.Models.Anime;
using Juro.Models.Videos;
using Juro.Utils;

namespace Juro.Providers.Anime.German;

public class Aniflix : IAnimeProvider
{
    private readonly HttpClient _http;
    private readonly IHttpClientFactory _httpClientFactory;

    public string Name => "Aniflix";

    public string Language => "de";

    public bool IsDubAvailableSeparately => false;

    public string BaseUrl => "https://aniflix.cc";

    /// <summary>
    /// Initializes an instance of <see cref="Aniflix"/>.
    /// </summary>
    public Aniflix(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient();
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Initializes an instance of <see cref="Aniflix"/>.
    /// </summary>
    public Aniflix(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider))
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="Aniflix"/>.
    /// </summary>
    public Aniflix() : this(Http.ClientProvider)
    {
    }

    public ValueTask<List<IAnimeInfo>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IAnimeInfo> GetAnimeInfoAsync(string animeId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<List<Episode>> GetEpisodesAsync(string animeId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<List<VideoSource>> GetVideosAsync(VideoServer server, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<List<VideoServer>> GetVideoServersAsync(string episodeId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}