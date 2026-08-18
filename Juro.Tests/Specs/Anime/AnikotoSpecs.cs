using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Juro.Providers.Anime;
using Xunit;

namespace Juro.Tests.Specs.Anime;

public class AnikotoSpecs
{
    [Fact]
    public async Task I_can_get_results_from_a_search_query()
    {
        // Arrange
        var provider = new Anikoto();

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
        var provider = new Anikoto();

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
        var provider = new Anikoto();

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
        var provider = new Anikoto();

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
        var provider = new Anikoto();

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
    public async Task Bleach_episode_282_sub_servers_return_playable_media()
    {
        // Arrange
        var provider = new Anikoto();
        var results = await provider.SearchAsync(
            "bleach",
            cancellationToken: TestContext.Current.CancellationToken
        );
        var bleach = results.First(x =>
            x.Title.Equals("Bleach", StringComparison.OrdinalIgnoreCase)
        );
        var episodes = await provider.GetEpisodesAsync(
            bleach.Id,
            cancellationToken: TestContext.Current.CancellationToken
        );
        var episode = episodes.Single(x => x.Number == 282);
        var servers = await provider.GetVideoServersAsync(
            episode.Id,
            cancellationToken: TestContext.Current.CancellationToken
        );
        var subServers = servers
            .Where(x => x.Name.StartsWith("Sub -", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Act / Assert
        await AnimeVideoAssertions.AssertEveryServerPlayableAsync(
            provider,
            subServers,
            "Anikoto / Bleach / Episode 282 / Sub",
            TestContext.Current.CancellationToken
        );
    }

    [Fact]
    public async Task I_can_get_a_playable_video()
    {
        await AnimeVideoAssertions.AssertPlayableVideoAsync(new Anikoto(), "Anikoto", "bleach");
    }
}
