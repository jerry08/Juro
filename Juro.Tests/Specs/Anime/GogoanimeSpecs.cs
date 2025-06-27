using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Httpz;
using Juro.Core.Models.Videos;
using Juro.Providers.Anime;
using Xunit;

namespace Juro.Tests.Specs.Anime;

[Obsolete("Gogoanime/Anitaku is officially dead.")]
public class GogoanimeSpecs
{
    [Theory]
    [InlineData("naruto")]
    [InlineData("spy x family")]
    [InlineData("jujutsu kaisen season 2")]
    [InlineData("eiyuu kyoushitsu")]
    public async Task I_can_get_results_from_a_search_query(string query)
    {
        // Arrange
        var provider = new Gogoanime();

        // Act
        var results = await provider.SearchAsync(query);

        // Assert
        results.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("naruto")]
    [InlineData("spy x family")]
    [InlineData("jujutsu kaisen season 2")]
    [InlineData("eiyuu kyoushitsu")]
    public async Task I_can_get_more_details_from_an_anime(string query)
    {
        // Arrange
        var provider = new Gogoanime();

        // Act
        var results = await provider.SearchAsync(query);

        // Assert
        results.Should().NotBeEmpty();

        // Act
        var animeInfo = await provider.GetAnimeInfoAsync(results[0].Id);

        // Assert
        animeInfo.Should().NotBeNull();
    }

    [Theory]
    [InlineData("naruto")]
    [InlineData("spy x family")]
    [InlineData("jujutsu kaisen season 2")]
    [InlineData("eiyuu kyoushitsu")]
    public async Task I_can_get_episode_results_from_an_anime(string query)
    {
        // Arrange
        var provider = new Gogoanime();

        // Act
        var results = await provider.SearchAsync(query);

        // Assert
        results.Should().NotBeEmpty();

        // Act
        var episodes = await provider.GetEpisodesAsync(results[0].Id);

        // Assert
        episodes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task I_can_get_video_server_results_from_an_episode()
    {
        // Arrange
        var provider = new Gogoanime();

        // Act
        var results = await provider.SearchAsync("naruto");

        // Assert
        results.Should().NotBeEmpty();

        // Act
        var episodes = await provider.GetEpisodesAsync(results[0].Id);

        // Assert
        episodes.Should().NotBeEmpty();

        // Act
        var videoServers = await provider.GetVideoServersAsync(episodes[0].Id);

        // Assert
        videoServers.Should().NotBeEmpty();
    }

    [Fact]
    public async Task I_can_get_video_results_from_a_video_server()
    {
        // Arrange
        var provider = new Gogoanime();

        // Act
        var results = await provider.SearchAsync("naruto");

        // Assert
        results.Should().NotBeEmpty();

        // Act
        var episodes = await provider.GetEpisodesAsync(results[0].Id);

        // Assert
        episodes.Should().NotBeEmpty();

        // Act
        var videoServers = await provider.GetVideoServersAsync(episodes[0].Id);

        // Assert
        videoServers.Should().NotBeEmpty();

        // Act
        var videos = await provider.GetVideosAsync(videoServers[0]);

        // Assert
        videos.Should().NotBeEmpty();
    }

    [Fact]
    public async Task I_can_get_video_results_from_all_video_servers()
    {
        // Arrange
        var provider = new Gogoanime();

        // Act
        var results = await provider.SearchAsync("naruto");

        // Assert
        results.Should().NotBeEmpty();

        // Act
        var episodes = await provider.GetEpisodesAsync(results[0].Id);

        // Assert
        episodes.Should().NotBeEmpty();

        // Act
        var videoServers = await provider.GetVideoServersAsync(episodes[0].Id);

        // Assert
        videoServers.Should().NotBeEmpty();

        // Act
        foreach (var videoServer in videoServers)
        {
            var videos = await provider.GetVideosAsync(videoServer);

            // Assert
            videos.Should().NotBeEmpty();
        }
    }

    [Fact]
    public async Task I_can_get_video_quality_results_from_all_hls_videos_in_all_video_servers()
    {
        // Arrange
        var provider = new Gogoanime();

        // Act
        var results = await provider.SearchAsync("naruto");

        // Assert
        results.Should().NotBeEmpty();

        // Act
        var episodes = await provider.GetEpisodesAsync(results[0].Id);

        // Assert
        episodes.Should().NotBeEmpty();

        // Act
        var videoServers = await provider.GetVideoServersAsync(episodes[0].Id);

        // Assert
        videoServers.Should().NotBeEmpty();

        // Act
        foreach (var videoServer in videoServers)
        {
            var videos = await provider.GetVideosAsync(videoServer);

            // Assert
            videos.Should().NotBeEmpty();

            // Act
            foreach (var video in videos.Where(x => x.Format is VideoType.M3u8))
            {
                var downloader = new HlsDownloader();
                var qualities = await downloader.GetQualitiesAsync(video.VideoUrl, video.Headers);

                // Assert
                qualities.Should().NotBeEmpty();
            }
        }
    }
}
