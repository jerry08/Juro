using System.Threading.Tasks;
using FluentAssertions;
using Juro.Providers.Anime;
using Xunit;

namespace Juro.Tests.Specs.Anime;

public class KickAssAnimeSpecs
{
    [Fact]
    public void Provider_has_expected_metadata()
    {
        // Arrange
        var provider = new KickAssAnime();

        // Assert
        provider.Key.Should().Be("KickAssAnime");
        provider.Name.Should().Be("KickAssAnime");
        provider.Language.Should().Be("en");
        provider.BaseUrl.Should().Be("https://kaa.lt");
    }

    [Fact(Skip = "kaa.lt returns a Cloudflare managed challenge from this test environment.")]
    public async Task I_can_get_results_from_a_search_query()
    {
        // Arrange
        var provider = new KickAssAnime();

        // Act
        var results = await provider.SearchAsync(
            "naruto",
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        results.Should().NotBeEmpty();
    }

    [Fact(Skip = "kaa.lt returns a Cloudflare managed challenge from this test environment.")]
    public async Task I_can_get_video_quality_results_from_m3u8_video()
    {
        await AnimeHlsAssertions.AssertReadableHlsQualitiesAsync(
            new KickAssAnime(),
            "KickAssAnime"
        );
    }
}
