using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Httpz;
using Juro.Clients;
using Juro.Providers.Anime;
using Juro.Providers.Aniskip;
using Juro.Providers.Manga;

namespace Juro.DemoConsole;

internal class Program
{
    static async Task Main()
    {
        Console.WriteLine("Hello, World!");

        await AnimeDemo();
        //await MangaDemo();
        //await MovieDemo();
    }

    private static async Task AnimeDemo()
    {
        var client = new AnimeClient();

        var allProviders = client.GetAllProviders();

        var provider = new Zoro();

        var animes = await provider.SearchAsync("spy x family");
        var animeInfo = await provider.GetAnimeInfoAsync(animes[0].Id);
        var episodes = await provider.GetEpisodesAsync(animes[0].Id);
        var videoServers = await provider.GetVideoServersAsync(episodes[0].Id);
        var videos = await provider.GetVideosAsync(videoServers[3]);

        var filePath = @"D:\Downloads\svs.mp4";

        //var downloader2 = new Downloader();
        //await downloader2.DownloadAsync(videos[0].VideoUrl, filePath, videos[0].Headers);

        var downloader = new HlsDownloader();
        var qualities = await downloader.GetQualitiesAsync(videos[0].VideoUrl, videos[0].Headers);
        await downloader.DownloadAllThenMergeAsync(qualities[0].Stream!, videos[0].Headers, filePath);
    }

    private static async Task MovieDemo()
    {
        var client = new MovieClient();
        var movies = await client.FlixHQ.SearchAsync("spongebob");

        var movieInfo = await client.FlixHQ.GetMediaInfoAsync(movies[0].Id);
        var servers = await client.FlixHQ.GetEpisodeServersAsync(movieInfo!.Episodes[0].Id, movieInfo!.Id);

        //Defaut
        var sources = await client.FlixHQ.GetEpisodeSourcesAsync(movieInfo!.Episodes[0].Id, movieInfo!.Id);
        //
        //var sources = await client.FlixHQ.GetEpisodeSourcesAsync(servers[0].Url, movieInfo!.Id);

        // Download the stream
        var fileName = $@"{Environment.CurrentDirectory}\test1.ts";

        //var downloader = new HlsDownloader();
        //
        //using var progress = new ConsoleProgress();
        //
        //var qualities = await downloader.GetHlsStreamMetadatasAsync(sources[0].VideoUrl, sources[0].Headers);
        //var stream = await qualities[0].Stream;
        //await downloader.DownloadAllTsThenMergeAsync(stream, sources[0].Headers, fileName, progress, 15);
    }

    private static async Task MangaDemo()
    {
        var client = new MangaClient();

        var allProviders = client.GetAllProviders();

        var provider = new MangaKatana();

        //var results = await provider.SearchAsync("Tomodachi Game");
        var results = await provider.SearchAsync("solo leveling");
        var mangaInfo = await provider.GetMangaInfoAsync(results[0].Id);
        var pages = await provider.GetChapterPagesAsync(mangaInfo.Chapters[0].Id);

        // Download the image
        var fileName = $@"{Environment.CurrentDirectory}\page1.png";

        var downloader = new Downloader();
        await downloader.DownloadAsync(
            pages[0].Image,
            fileName,
            headers: pages[0].HeaderForImage
        );
    }
}