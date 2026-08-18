using System;
using System.Threading.Tasks;
using FluentAssertions;
using Juro.Providers.Anime;
using Xunit;

namespace Juro.Tests.Specs.Anime;

/// <summary>
/// animegg.org resets connections at the TLS level for some networks/regions,
/// so every spec runs through
/// <see cref="LiveSiteGuard.SkipWhenBlockedAtTransportAsync"/> and is skipped
/// (not failed) when the site is unreachable from this environment.
/// </summary>
public class AnimeGGSpecs
{
    private static Task GuardAsync(Func<Task> body) =>
        LiveSiteGuard.SkipWhenBlockedAtTransportAsync("AnimeGG", body);

    [Fact]
    public Task I_can_get_results_from_a_search_query() =>
        GuardAsync(async () =>
        {
            // Arrange
            var provider = new AnimeGG();

            // Act
            var results = await provider.SearchAsync(
                "naruto",
                cancellationToken: TestContext.Current.CancellationToken
            );

            // Assert
            results.Should().NotBeEmpty();
        });

    [Fact]
    public Task I_can_get_more_details_from_an_anime() =>
        GuardAsync(async () =>
        {
            // Arrange
            var provider = new AnimeGG();

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
        });

    [Fact]
    public Task I_can_get_episode_results_from_an_anime() =>
        GuardAsync(async () =>
        {
            // Arrange
            var provider = new AnimeGG();

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
        });

    [Fact]
    public Task I_can_get_video_server_results_from_an_episode() =>
        GuardAsync(async () =>
        {
            // Arrange
            var provider = new AnimeGG();

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
        });

    [Fact]
    public Task I_can_get_video_results_from_a_video_server() =>
        GuardAsync(async () =>
        {
            // Arrange
            var provider = new AnimeGG();

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
        });

    [Fact]
    public Task I_can_get_a_playable_video() =>
        GuardAsync(() => AnimeVideoAssertions.AssertPlayableVideoAsync(new AnimeGG(), "AnimeGG"));
}
