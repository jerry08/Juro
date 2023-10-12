using System.Linq;
using FluentAssertions;
using Juro.Clients;
using Xunit;

namespace Juro.Tests;

public class MainSpecs
{
    [Fact]
    public void All_providers_has_a_unique_key()
    {
        // Arrange
        var client = new AnimeClient();

        // Act
        var results = client.GetAllProviders().GroupBy(x => x.Key).Where(x => x.Count() > 1);

        // Assert
        results.Should().HaveCount(0);
    }
}
