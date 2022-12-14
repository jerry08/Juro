using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Juro.Models.Movies;
using Juro.Models.Videos;

namespace Juro.Providers.Movies;

public abstract class MovieParser<TMovieResult, TMovieInfo, TEpisodeServer, TVideoSource>
{
    public HttpClient _http;
    
    public abstract string Name { get; set; }

    public virtual string BaseUrl => default!;

    public virtual string Logo => default!;

    public MovieParser(HttpClient http)
        => _http = http;

    public abstract Task<List<TMovieResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default!);

    public abstract Task<TMovieInfo> GetMediaInfoAsync(
        string mediaId,
        CancellationToken cancellationToken = default!);

    public abstract Task<List<TEpisodeServer>> GetEpisodeServersAsync(
        string episodeId,
        string mediaId,
        CancellationToken cancellationToken = default!);

    public abstract Task<List<TVideoSource>> GetEpisodeSourcesAsync(
        string episodeId,
        string mediaId,
        StreamingServers server = StreamingServers.UpCloud,
        CancellationToken cancellationToken = default!);
}

public abstract class MovieParser<TMovieResult>
    : MovieParser<TMovieResult, MovieInfo, EpisodeServer, VideoSource>
{
    public MovieParser(HttpClient http) : base(http) { }
}

public abstract class MovieParser<TMovieResult, TMovieInfo>
    : MovieParser<TMovieResult, TMovieInfo, EpisodeServer, VideoSource>
{
    public MovieParser(HttpClient http) : base(http) { }
}

public abstract class MovieParser<TMovieResult, TMovieInfo, TEpisodeServer>
    : MovieParser<TMovieResult, TMovieInfo, TEpisodeServer, VideoSource>
{
    public MovieParser(HttpClient http) : base(http) { }
}

public abstract class MovieParser
    : MovieParser<MovieResult, MovieInfo, EpisodeServer, VideoSource>
{
    public MovieParser(HttpClient http) : base(http) { }
}