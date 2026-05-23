using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Httpz;
using Juro.Core.Models.Videos;
using Juro.Core.Providers;
using Xunit;

namespace Juro.Tests.Specs.Anime;

internal static class AnimeHlsAssertions
{
    public static async Task AssertReadableHlsQualitiesAsync(
        IAnimeProvider provider,
        string providerName,
        string query = "spy x family"
    )
    {
        var results = await provider.SearchAsync(
            query,
            cancellationToken: TestContext.Current.CancellationToken
        );

        results.Should().NotBeEmpty();

        var episodes = await provider.GetEpisodesAsync(
            results[0].Id,
            cancellationToken: TestContext.Current.CancellationToken
        );

        episodes.Should().NotBeEmpty();

        var videoServers = await provider.GetVideoServersAsync(
            episodes[0].Id,
            cancellationToken: TestContext.Current.CancellationToken
        );

        videoServers.Should().NotBeEmpty();

        var downloader = new HlsDownloader();
        var hlsErrors = new List<string>();
        var foundQualities = false;

        foreach (var videoServer in videoServers)
        {
            var videos = await provider.GetVideosAsync(
                videoServer,
                cancellationToken: TestContext.Current.CancellationToken
            );

            videos.Should().NotBeEmpty();

            foreach (
                var hlsVideo in videos.Where(x =>
                    x.Format is VideoType.M3u8 or VideoType.Hls
                    || x.VideoUrl.Contains(".m3u8", System.StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                try
                {
                    var qualities = await downloader.GetQualitiesAsync(
                        hlsVideo.VideoUrl,
                        hlsVideo.Headers,
                        TestContext.Current.CancellationToken
                    );

                    if (qualities.Count > 0)
                    {
                        foundQualities = true;
                        break;
                    }
                }
                catch (HttpRequestException exception)
                {
                    hlsErrors.Add(exception.Message);
                }
            }

            if (foundQualities)
            {
                break;
            }
        }

        if (!foundQualities)
        {
            var reason =
                hlsErrors.Count > 0
                    ? string.Join("; ", hlsErrors)
                    : $"{providerName} did not return any M3U8 video sources.";

            Assert.Skip($"{providerName} did not expose a readable HLS manifest: {reason}");
        }
    }
}
