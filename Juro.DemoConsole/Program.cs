using System;
using System.Threading.Tasks;
using Httpz;
using Juro.Clients;
using Juro.Providers.Anime;
using Juro.Providers.Aniskip;
using Juro.Providers.Manga;
using Juro.Providers.Movie;

namespace Juro.DemoConsole;

internal static class Program
{
    static async Task Main()
    {
        //var aniskipClient = new AniskipClient();
        //
        //var timeStamps = await aniskipClient.GetAsync(52991, 1, 1559891 / 1000);

        await AnimeDemo();
        await MangaDemo();
        //await MovieDemo();
    }

    private static async Task AnimeDemo()
    {
        var client = new AnimeClient();

        var svs = System.Text.Encoding.UTF8.GetBytes("aR5KNujcZTnHarYFng2Fg64MsRRQZ47gB");

        //var sources = "U2FsdGVkX1hfJKkYFdEnPZNIbXi7PmgxG3UIqUql5MH16lJa/m6/UhnAbEC3YVx9hmvaht8CWUG+SvFFEhiqlnSudkXwCJWy/cHrBU5Xmn+kJhaqAYBxpn4bErrjafrwqHStCwYrNmbzDlRlwTvfa+0dMLxp+7jjmVZtKucrfdQDaIm/FPeAaFKOuZ5dJY0PbYHQe1/GgG20fWJi6r5LkLOit2TKn6u4UES+nt5crLMr4R7DK3gxCnfnCzmpP+zprSZqz1fFS+IVybrBLW46nyuF1YI7H8fjCDu90YxRHmyIiuvobTFKCDEu90zDpluapPvN8lLIYpN0PEtKyJ7fWd8RDaM8K99aASwn9GE2t6m4mgPT51pJ6ZocF7tVgTvMWc0wejrZyhIVa/5cHql3A9B+4pf9fm26xgX5SW9g==";
        //sources = sources.Trim();
        //var ss = Convert.FromBase64String(sources);
        //var sourcesArray = sources!.Select(x => char.ConvertFromUtf32(x)).ToList();

        var allProviders = client.GetAllProviders();

        //var provider = new OtakuDesu();
        var provider = new Gogoanime();

        //var test = await provider.GetPopularAsync();

        //var test = await provider.SearchByGenreAsync("action");
        //var animes = await provider.SearchAsync("spy x family");
        var animes = await provider.SearchAsync("jujutsu kaisen season 2");
        //var animes = await provider.SearchAsync("eiyuu kyoushitsu");
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
        var provider = new FlixHQ();
        var result = await provider.SearchAsync("ano");
        var movieInfo = await provider.GetMediaInfoAsync(result[0].Id);
        var episodes = await provider.GetEpisodeServersAsync(result[0].Id, movieInfo.Id);
    }

    //private static async Task MovieDemo()
    //{
    //    var client = new MovieClient();
    //    var movies = await client.FlixHQ.SearchAsync("spongebob");
    //
    //    var movieInfo = await client.FlixHQ.GetMediaInfoAsync(movies[0].Id);
    //    var servers = await client.FlixHQ.GetEpisodeServersAsync(movieInfo!.Episodes[0].Id, movieInfo!.Id);
    //
    //    //Defaut
    //    var sources = await client.FlixHQ.GetEpisodeSourcesAsync(movieInfo!.Episodes[0].Id, movieInfo!.Id);
    //    //
    //    //var sources = await client.FlixHQ.GetEpisodeSourcesAsync(servers[0].Url, movieInfo!.Id);
    //
    //    // Download the stream
    //    var fileName = $@"{Environment.CurrentDirectory}\test1.ts";
    //
    //    //var downloader = new HlsDownloader();
    //    //
    //    //using var progress = new ConsoleProgress();
    //    //
    //    //var qualities = await downloader.GetHlsStreamMetadatasAsync(sources[0].VideoUrl, sources[0].Headers);
    //    //var stream = await qualities[0].Stream;
    //    //await downloader.DownloadAllTsThenMergeAsync(stream, sources[0].Headers, fileName, progress, 15);
    //}

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
        await downloader.DownloadAsync(
            pages[0].Image,
            fileName,
            headers: pages[0].HeaderForImage
        );
    }
}