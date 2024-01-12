using System.Threading.Tasks;
using FluentAssertions;
using Juro.Providers.Anime.Indonesian;
using Xunit;

namespace Juro.Tests.Specs;

public class OtakuDesuSpecs
{
    [Fact]
    public async Task I_can_get_results_from_a_search_query()
    {
        // Arrange
        var provider = new OtakuDesu();

        // Act
        var results = await provider.SearchAsync("naruto");

        // Assert
        results.Should().NotBeEmpty();
    }

    [Fact]
    public async Task I_can_get_more_details_from_an_anime()
    {
        // Arrange
        var provider = new OtakuDesu();

        // Act
        var results = await provider.SearchAsync("naruto");

        // Assert
        results.Should().NotBeEmpty();

        // Act
        var animeInfo = await provider.GetAnimeInfoAsync(results[0].Id);

        // Assert
        animeInfo.Should().NotBeNull();
    }

    [Fact]
    public async Task I_can_get_episode_results_from_an_anime()
    {
        // Arrange
        var provider = new OtakuDesu();

        // Act
        var results = await provider.SearchAsync("naruto");

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
        var provider = new OtakuDesu();

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
        var provider = new OtakuDesu();

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
        var provider = new OtakuDesu();

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
}
