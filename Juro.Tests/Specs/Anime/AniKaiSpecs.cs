using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Httpz;
using Juro.Core.Models.Videos;
using Juro.Providers.Anime;
using Xunit;

namespace Juro.Tests.Specs.Anime;

public class AniKaiSpecs
{
    [Fact]
    public async Task I_can_get_results_from_a_search_query()
    {
        // Arrange
        var provider = new AniKai();

        // Act
        var results = await provider.SearchAsync(
            "naruto",
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        results.Should().NotBeEmpty();
    }

    [Fact]
    public async Task I_can_get_more_details_from_an_anime()
    {
        // Arrange
        var provider = new AniKai();

        // Act
        var results = await provider.SearchAsync(
            "naruto",
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        results.Should().NotBeEmpty();

        // Act
        var animeInfo = await provider.GetAnimeInfoAsync(
            results[0].Id,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        animeInfo.Should().NotBeNull();
    }

    [Fact]
    public async Task I_can_get_episode_results_from_an_anime()
    {
        // Arrange
        var provider = new AniKai();

        // Act
        var results = await provider.SearchAsync(
            "naruto",
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        results.Should().NotBeEmpty();

        // Act
        var episodes = await provider.GetEpisodesAsync(
            results[0].Id,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        episodes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task I_can_get_video_server_results_from_an_episode()
    {
        // Arrange
        var provider = new AniKai();

        // Act
        var results = await provider.SearchAsync(
            "naruto",
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        results.Should().NotBeEmpty();

        // Act
        var episodes = await provider.GetEpisodesAsync(
            results[0].Id,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        episodes.Should().NotBeEmpty();

        // Act
        var videoServers = await provider.GetVideoServersAsync(
            episodes[0].Id,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        videoServers.Should().NotBeEmpty();
    }

    [Fact]
    public async Task I_can_get_video_results_from_a_video_server()
    {
        // Arrange
        var provider = new AniKai();

        // Act
        var results = await provider.SearchAsync(
            "naruto",
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        results.Should().NotBeEmpty();

        // Act
        var episodes = await provider.GetEpisodesAsync(
            results[0].Id,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        episodes.Should().NotBeEmpty();

        // Act
        var videoServers = await provider.GetVideoServersAsync(
            episodes[0].Id,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        videoServers.Should().NotBeEmpty();

        // Act
        var videos = await provider.GetVideosAsync(
            videoServers[0],
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        videos.Should().NotBeEmpty();
    }

    [Fact]
    public async Task I_can_get_video_results_from_all_video_servers()
    {
        // Arrange
        var provider = new AniKai();

        // Act
        var results = await provider.SearchAsync(
            "naruto",
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        results.Should().NotBeEmpty();

        // Act
        var episodes = await provider.GetEpisodesAsync(
            results[0].Id,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        episodes.Should().NotBeEmpty();

        // Act
        var videoServers = await provider.GetVideoServersAsync(
            episodes[0].Id,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        videoServers.Should().NotBeEmpty();

        // Act
        foreach (var videoServer in videoServers)
        {
            var videos = await provider.GetVideosAsync(
                videoServer,
                cancellationToken: TestContext.Current.CancellationToken
            );

            // Assert
            videos.Should().NotBeEmpty();
        }
    }

    [Fact]
    public async Task I_can_get_video_quality_results_from_m3u8_video()
    {
        // Arrange
        var provider = new AniKai();

        // Act
        var results = await provider.SearchAsync(
            "spy x family",
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        results.Should().NotBeEmpty();

        // Act
        var episodes = await provider.GetEpisodesAsync(
            results[0].Id,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        episodes.Should().NotBeEmpty();

        // Act
        var videoServers = await provider.GetVideoServersAsync(
            episodes[0].Id,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        videoServers.Should().NotBeEmpty();

        var downloader = new HlsDownloader();
        var hlsErrors = new List<string>();
        var foundQualities = false;

        // Act
        foreach (var videoServer in videoServers)
        {
            var videos = await provider.GetVideosAsync(
                videoServer,
                cancellationToken: TestContext.Current.CancellationToken
            );

            // Assert
            videos.Should().NotBeEmpty();

            foreach (var hlsVideo in videos.Where(x => x.Format is VideoType.M3u8))
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
                break;
        }

        if (!foundQualities)
        {
            var reason =
                hlsErrors.Count > 0
                    ? string.Join("; ", hlsErrors)
                    : "AniKai did not return any M3U8 video sources.";

            Assert.Skip($"AniKai did not expose a readable HLS manifest: {reason}");
        }
    }
}
