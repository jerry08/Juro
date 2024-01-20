using System.Linq;
using FluentAssertions;
using Juro.Clients;
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
}
