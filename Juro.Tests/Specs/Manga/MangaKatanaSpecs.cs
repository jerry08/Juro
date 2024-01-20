using System.Threading.Tasks;
using FluentAssertions;
using Juro.Providers.Manga;
using Xunit;

namespace Juro.Tests.Specs.Manga;

public class MangaKatanaSpecs
{
    [Theory]
    [InlineData("solo leveling")]
    public async Task I_can_get_results_from_a_search_query(string query)
    {
        // Arrange
        var provider = new MangaKatana();

        // Act
        var results = await provider.SearchAsync(query);

        // Assert
        results.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("solo leveling")]
    public async Task I_can_get_details_from_a_manga(string query)
    {
        // Arrange
        var provider = new MangaKatana();

        // Act
        var results = await provider.SearchAsync(query);

        // Assert
        results.Should().NotBeEmpty();

        // Act
        var mangaInfo = await provider.GetMangaInfoAsync(results[0].Id);

        // Assert
        mangaInfo.Should().NotBeNull();
    }

    [Theory]
    [InlineData("solo leveling")]
    public async Task I_can_get_chapter_results_from_a_manga(string query)
    {
        // Arrange
        var provider = new MangaKatana();

        // Act
        var results = await provider.SearchAsync(query);

        // Assert
        results.Should().NotBeEmpty();

        // Act
        var mangaInfo = await provider.GetMangaInfoAsync(results[0].Id);

        // Assert
        mangaInfo.Should().NotBeNull();

        // Assert
        mangaInfo.Chapters.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("solo leveling")]
    public async Task I_can_get_chapter_pages_from_a_manga(string query)
    {
        // Arrange
        var provider = new MangaKatana();

        // Act
        var results = await provider.SearchAsync(query);

        // Assert
        results.Should().NotBeEmpty();

        // Act
        var mangaInfo = await provider.GetMangaInfoAsync(results[0].Id);

        // Assert
        mangaInfo.Should().NotBeNull();

        // Assert
        mangaInfo.Chapters.Should().NotBeEmpty();

        // Act
        var pages = await provider.GetChapterPagesAsync(mangaInfo.Chapters[0].Id);

        // Assert
        pages.Should().NotBeEmpty();
    }
}
