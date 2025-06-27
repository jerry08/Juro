using System;
using System.Threading.Tasks;
using Httpz;
using Juro.Clients;
using Juro.Providers.Anime;
using Juro.Providers.Manga;
using Juro.Providers.Movie;

namespace Juro.DemoConsole;

internal static class Program
{
    static async Task Main()
    {
        await AnimeDemo();
        await MangaDemo();
        //await MovieDemo();
    }

    private static async Task AnimeDemo()
    {
        var client = new AnimeClient();

        var allProviders = client.GetAllProviders();

        var provider = new Aniwatch();

        var animes = await provider.SearchAsync("jujutsu kaisen season 2");
        var animeInfo = await provider.GetAnimeInfoAsync(animes[0].Id);
        var episodes = await provider.GetEpisodesAsync(animes[0].Id);
        var videoServers = await provider.GetVideoServersAsync(episodes[0].Id);
        var videos = await provider.GetVideosAsync(videoServers[1]);

        var filePath = @"D:\Downloads\svs.mp4";

        var downloader = new HlsDownloader();
        var qualities = await downloader.GetQualitiesAsync(videos[0].VideoUrl, videos[0].Headers);
        await downloader.DownloadAllThenMergeAsync(
            qualities[0].Stream!,
            videos[0].Headers,
            filePath
        );
    }

    private static async Task MovieDemo()
    {
        var provider = new FlixHQ();
        var result = await provider.SearchAsync("ano");
        var movieInfo = await provider.GetMediaInfoAsync(result[0].Id);
        var episodes = await provider.GetEpisodeServersAsync(result[0].Id, movieInfo.Id);
    }

    private static async Task MangaDemo()
    {
        var client = new MangaClient();

        var allProviders = client.GetAllProviders();

        var provider = new MangaPill();

        //var results = await provider.SearchAsync("Tomodachi Game");
        var results = await provider.SearchAsync("solo leveling");
        var mangaInfo = await provider.GetMangaInfoAsync(results[0].Id);
        var pages = await provider.GetChapterPagesAsync(mangaInfo.Chapters[0].Id);

        // Download the image
        var fileName = $@"{Environment.CurrentDirectory}\page1.png";

        var downloader = new Downloader();
        await downloader.DownloadAsync(pages[0].Image, fileName, headers: pages[0].Headers);
    }
}
