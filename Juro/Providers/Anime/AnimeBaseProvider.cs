using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Juro.Extractors;
using Juro.Models;
using Juro.Models.Videos;

namespace Juro.Providers.Anime;

public class AnimeBaseProvider : IVideoExtractorProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AnimeBaseProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public virtual IVideoExtractor? GetVideoExtractor(VideoServer server)
    {
        var domain = new Uri(server.Embed.Url).Host;
        if (domain.StartsWith("www."))
            domain = domain.Substring(4);

        return domain.ToLower() switch
        {
            "filemoon.to" or "filemoon.sx" => new FilemoonExtractor(_httpClientFactory),
            "rapid-cloud.co" => new RapidCloudExtractor(_httpClientFactory),
            "streamtape.com" => new StreamTapeExtractor(_httpClientFactory),
            "vidstream.pro" => new VidStreamExtractor(_httpClientFactory),
            "mp4upload.com" => new Mp4uploadExtractor(_httpClientFactory),
            "playtaku.net" or "goone.pro" => new GogoCDNExtractor(_httpClientFactory),
            "alions.pro" => new ALionsExtractor(_httpClientFactory),
            "awish.pro" => new AWishExtractor(_httpClientFactory),
            "dood.wf" => new DoodExtractor(_httpClientFactory),
            "ok.ru" => new OkRuExtractor(_httpClientFactory),
            "streamlare.com" => null,
            _ => null
        };
    }

    /// <inheritdoc />
    public virtual async ValueTask<List<VideoSource>> GetVideosAsync(
        VideoServer server,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.IsWellFormedUriString(server.Embed.Url, UriKind.Absolute))
            return new();

        var extractor = GetVideoExtractor(server);
        if (extractor is null)
            return new();

        return await extractor.ExtractAsync(server.Embed.Url, cancellationToken);
    }
}