using System.Threading.Tasks;
using FluentAssertions;
using Juro.Providers.Anime;
using Xunit;

namespace Juro.Tests;

public class SearchSpecs
{
    [Fact]
    public async Task I_can_get_results_from_a_search_query()
    {
        // Arrange
        var client = new Gogoanime();

        // Act
        var results = await client.SearchAsync("naruto");

        // Assert
        results.Should().HaveCountGreaterOrEqualTo(1);
    }
}
