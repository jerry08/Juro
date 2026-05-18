using System.Linq;
using FluentAssertions;
using Juro.Clients;
using Juro.Providers.Anime;
using Xunit;

namespace Juro.Tests.Specs;

public class MainSpecs
{
    [Fact]
    public void Each_provider_has_a_unique_key()
    {
        // Arrange
        var client = new AnimeClient();

        // Act
        var results = client.GetAllProviders().GroupBy(x => x.Key).Where(x => x.Count() > 1);

        // Assert
        results.Should().HaveCount(0);
    }

    [Fact]
    public void Anime_client_discovers_added_anime_providers()
    {
        // Arrange
        _ = typeof(AnimeGG);
        var client = new AnimeClient();

        // Act
        var providerTypes = client.GetAllProviders().Select(x => x.GetType()).ToList();

        // Assert
        providerTypes.Should().Contain(typeof(AnimeGG));
        providerTypes.Should().Contain(typeof(Miruro));
        providerTypes.Should().Contain(typeof(KickAssAnime));
    }
}
