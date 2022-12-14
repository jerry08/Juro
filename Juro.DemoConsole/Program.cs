using Juro.Clients;

namespace Juro.DemoConsole;

internal class Program
{
    static async Task Main()
    {
        Console.WriteLine("Hello, World!");

        //await TestMovie();
        await TestManga();
    }

    static async Task TestMovie()
    {
        var client = new MovieClient();
        var movies = await client.FlixHQ.SearchAsync("anohana");

        var movieInfo = await client.FlixHQ.GetMediaInfoAsync(movies[0].Id);
        var servers = await client.FlixHQ.GetEpisodeServersAsync(movieInfo!.Episodes[0].Id, movieInfo!.Id);
        var sources = await client.FlixHQ.GetEpisodeSourcesAsync(movieInfo!.Episodes[0].Id, movieInfo!.Id);
    }

    static async Task TestManga()
    {
        var client = new MangaClient();
        //var results = await client.MangaKakalot.SearchAsync("Tomodachi Game");
        var results = await client.Mangadex.SearchAsync("solo leveling");
        var mangaInfo = await client.Mangadex.GetMangaInfoAsync(results[0].Id);
        var chapters = await client.Mangadex.GetChapterPagesAsync(mangaInfo.Chapters[0].Id);
    }
}