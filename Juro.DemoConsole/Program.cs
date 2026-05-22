using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Httpz;
using Httpz.Hls;
using Juro.Clients;
using Juro.Core.Models;
using Juro.Core.Models.Anime;
using Juro.Core.Models.Manga;
using Juro.Core.Models.Movie;
using Juro.Core.Models.Videos;
using Juro.Core.Providers;
using Juro.Utils;
using Spectre.Console;
using AnimeEpisode = Juro.Core.Models.Anime.Episode;
using MovieEpisode = Juro.Core.Models.Movie.Episode;

namespace Juro.DemoConsole;

internal enum DemoAction
{
    AnimeProvider,
    MangaProvider,
    MovieProvider,
    AnimeApi,
    Exit,
}

internal static class Program
{
    private const int DisplayLimit = 20;
    private static readonly string DownloadsDirectory = Path.Combine(
        Environment.CurrentDirectory,
        "Downloads"
    );

    static async Task Main()
    {
        Console.Title = "Juro Demo Console";
        _ = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "Juro.Providers.dll"));
        AssemblyEx.LoadReferencedAssemblies();

        AnsiConsole.Write(new FigletText("Juro Demo").Color(Color.Cyan1));
        AnsiConsole.MarkupLine("[grey]Interactive playground for provider and API demos.[/]");
        AnsiConsole.Write(new Rule("[grey]Session[/]"));

        var continueSession = true;

        while (continueSession)
        {
            var action = PromptAction();
            if (action == DemoAction.Exit)
            {
                break;
            }

            try
            {
                await RunActionAsync(action);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(
                    ex,
                    ExceptionFormats.ShortenEverything | ExceptionFormats.ShowLinks
                );
            }

            AnsiConsole.WriteLine();
            continueSession = AnsiConsole.Confirm("[grey]Run another demo?[/]", true);
        }

        AnsiConsole.MarkupLine("[green]Demo session completed.[/]");
    }

    private static DemoAction PromptAction() =>
        AnsiConsole.Prompt(
            new SelectionPrompt<DemoAction>()
                .Title("[bold]Choose demo[/]")
                .PageSize(10)
                .UseConverter(GetActionLabel)
                .AddChoices(Enum.GetValues<DemoAction>())
        );

    private static string GetActionLabel(DemoAction action) =>
        action switch
        {
            DemoAction.AnimeProvider => "Anime Provider",
            DemoAction.MangaProvider => "Manga Provider",
            DemoAction.MovieProvider => "Movie Provider",
            DemoAction.AnimeApi => "Anime Web API",
            DemoAction.Exit => "Exit",
            _ => action.ToString(),
        };

    private static Task RunActionAsync(DemoAction action) =>
        action switch
        {
            DemoAction.AnimeProvider => RunAnimeProviderDemoAsync(),
            DemoAction.MangaProvider => RunMangaProviderDemoAsync(),
            DemoAction.MovieProvider => RunMovieProviderDemoAsync(),
            DemoAction.AnimeApi => RunAnimeApiDemoAsync(),
            _ => Task.CompletedTask,
        };

    private static async Task RunAnimeProviderDemoAsync()
    {
        var providers = OrderProviders(new AnimeClient().GetAllProviders(), "Aniwave");
        if (providers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No local anime providers were discovered.[/]");
            return;
        }

        RenderSourceProviderTable("Anime Providers", providers);

        var provider = PromptChoice(
            "[bold]Select anime provider[/]",
            providers,
            FormatProviderLabel
        );

        var query = AskRequired("[cyan]Anime search query[/]", "spy x family");

        var results = await RunWithStatusAsync(
            $"[cyan]Searching {Escape(provider.Name)}[/]",
            async () => await provider.SearchAsync(query)
        );

        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No anime results found.[/]");
            return;
        }

        var limitedResults = results.Take(DisplayLimit).ToList();
        RenderAnimeSearchTable(limitedResults, results.Count, provider.Name);

        var selectedAnime = PromptChoice(
            "[bold]Select anime[/]",
            limitedResults,
            anime => $"{Escape(Clip(anime.Title, 52))} [grey]({Escape(Clip(anime.Id, 30))})[/]"
        );

        var animeInfo = await RunWithStatusAsync(
            "[cyan]Loading anime details[/]",
            async () => await provider.GetAnimeInfoAsync(selectedAnime.Id)
        );

        var episodes = await RunWithStatusAsync(
            "[cyan]Loading episodes[/]",
            async () => await provider.GetEpisodesAsync(selectedAnime.Id)
        );

        RenderAnimeDetails(provider.Name, animeInfo, episodes);

        await RunVideoInspectionAsync(
            provider,
            animeInfo.Title,
            episodes,
            episode => $"Episode {episode.Number:0.##} - {Escape(Clip(episode.Name, 45))}"
        );
    }

    private static async Task RunMangaProviderDemoAsync()
    {
        var providers = OrderProviders(new MangaClient().GetAllProviders(), "AsuraScans");
        if (providers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No local manga providers were discovered.[/]");
            return;
        }

        RenderSourceProviderTable("Manga Providers", providers);

        var provider = PromptChoice(
            "[bold]Select manga provider[/]",
            providers,
            FormatProviderLabel
        );

        var query = AskRequired("[cyan]Manga search query[/]", "solo leveling");

        var results = await RunWithStatusAsync(
            $"[cyan]Searching {Escape(provider.Name)}[/]",
            async () => await provider.SearchAsync(query)
        );

        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No manga results found.[/]");
            return;
        }

        var limitedResults = results.Take(DisplayLimit).ToList();
        RenderMangaSearchTable(limitedResults, results.Count, provider.Name);

        var selectedManga = PromptChoice(
            "[bold]Select manga[/]",
            limitedResults,
            manga => $"{Escape(Clip(manga.Title, 52))} [grey]({Escape(Clip(manga.Id, 30))})[/]"
        );

        var mangaInfo = await RunWithStatusAsync(
            "[cyan]Loading manga details[/]",
            async () => await provider.GetMangaInfoAsync(selectedManga.Id)
        );

        RenderMangaDetails(provider.Name, mangaInfo);

        if (mangaInfo.Chapters.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No chapters available.[/]");
            return;
        }

        var limitedChapters = mangaInfo.Chapters.Take(DisplayLimit).ToList();
        RenderMangaChapterTable(limitedChapters, mangaInfo.Chapters.Count);

        var selectedChapter = PromptChoice(
            "[bold]Select chapter[/]",
            limitedChapters,
            chapter => $"Chapter {chapter.Number:0.##} - {Escape(Clip(chapter.Title, 45))}"
        );

        var pages = await RunWithStatusAsync(
            "[cyan]Loading chapter pages[/]",
            async () => await provider.GetChapterPagesAsync(selectedChapter.Id)
        );

        RenderMangaPagesTable(pages);
        await TryDownloadMangaPagesAsync(
            pages,
            $"{mangaInfo.Title}-chapter-{selectedChapter.Number:0.##}"
        );
    }

    private static async Task RunMovieProviderDemoAsync()
    {
        var providers = OrderProviders(new MovieClient().GetAllProviders(), "FlixHQ");
        if (providers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No local movie providers were discovered.[/]");
            return;
        }

        RenderSourceProviderTable("Movie Providers", providers);

        var provider = PromptChoice(
            "[bold]Select movie provider[/]",
            providers,
            FormatProviderLabel
        );

        var query = AskRequired("[cyan]Movie search query[/]", "spirited away");

        var results = await RunWithStatusAsync(
            $"[cyan]Searching {Escape(provider.Name)}[/]",
            async () => await provider.SearchAsync(query)
        );

        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No movie results found.[/]");
            return;
        }

        var limitedResults = results.Take(DisplayLimit).ToList();
        RenderMovieSearchTable(limitedResults, results.Count, provider.Name);

        var selectedMovie = PromptChoice(
            "[bold]Select movie/series[/]",
            limitedResults,
            movie => $"{Escape(Clip(movie.Title, 50))} [grey]({Escape(movie.Type.ToString())})[/]"
        );

        var mediaInfo = await RunWithStatusAsync(
            "[cyan]Loading media details[/]",
            async () => await provider.GetMediaInfoAsync(selectedMovie.Id)
        );

        RenderMovieDetails(provider.Name, mediaInfo);

        if (mediaInfo.Episodes.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No episodes available.[/]");
            return;
        }

        var limitedEpisodes = mediaInfo.Episodes.Take(DisplayLimit).ToList();
        RenderMovieEpisodeTable(limitedEpisodes, mediaInfo.Episodes.Count);

        var selectedEpisode = PromptChoice(
            "[bold]Select episode[/]",
            limitedEpisodes,
            episode =>
                $"S{episode.Season:00}E{episode.Number:00} - {Escape(Clip(episode.Title, 45))}"
        );

        var servers = await RunWithStatusAsync(
            "[cyan]Loading video servers[/]",
            async () => await provider.GetEpisodeServersAsync(selectedEpisode.Id, mediaInfo.Id)
        );

        if (servers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No servers found for selected episode.[/]");
            return;
        }

        RenderVideoServerTable(servers);

        var selectedServer = PromptChoice(
            "[bold]Select server[/]",
            servers,
            server => $"{Escape(server.Name)} [grey]({Escape(Clip(server.Embed.Url, 55))})[/]"
        );

        var videoSources = await RunWithStatusAsync(
            "[cyan]Loading video sources[/]",
            async () => await provider.GetVideosAsync(selectedServer)
        );

        RenderVideoSourceTable(videoSources);
        await TryDownloadVideoSourcesAsync(
            videoSources,
            $"{mediaInfo.Title}-s{selectedEpisode.Season:00}e{selectedEpisode.Number:00}"
        );
    }

    private static async Task RunAnimeApiDemoAsync()
    {
        var baseUrl = AskRequired("[cyan]API base URL[/]", "https://juro.mobiv.org/api");

        var apiClient = new AnimeApiClient(baseUrl);

        var providers = await RunWithStatusAsync(
            "[cyan]Loading providers from API[/]",
            async () => await apiClient.GetProvidersAsync()
        );

        var animeProviders = providers
            .Where(provider => provider.Type == ProviderType.Anime)
            .OrderBy(provider => provider.Name)
            .ToList();

        if (animeProviders.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No anime providers returned by API.[/]");
            return;
        }

        RenderApiProviderTable(animeProviders);

        var selectedProvider = PromptChoice(
            "[bold]Select API provider[/]",
            animeProviders,
            provider =>
                $"{Escape(provider.Name)} [grey]({Escape(provider.Key)} | {Escape(provider.Language)})[/]"
        );

        apiClient.ProviderKey = selectedProvider.Key;

        var query = AskRequired("[cyan]Anime search query[/]", "violet evergarden");
        var searchResults = await RunWithStatusAsync(
            "[cyan]Searching through API[/]",
            async () => await apiClient.SearchAsync(query)
        );

        if (searchResults.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No anime results found via API.[/]");
            return;
        }

        var limitedResults = searchResults.Take(DisplayLimit).ToList();
        RenderAnimeSearchTable(limitedResults, searchResults.Count, selectedProvider.Name);

        var selectedAnime = PromptChoice(
            "[bold]Select anime[/]",
            limitedResults,
            anime => $"{Escape(Clip(anime.Title, 52))} [grey]({Escape(Clip(anime.Id, 30))})[/]"
        );

        var animeInfo = await RunWithStatusAsync(
            "[cyan]Loading anime details[/]",
            async () => await apiClient.GetAsync(selectedAnime.Id)
        );

        var episodes = await RunWithStatusAsync(
            "[cyan]Loading episodes[/]",
            async () => await apiClient.GetEpisodesAsync(selectedAnime.Id)
        );

        RenderAnimeDetails(selectedProvider.Name, animeInfo, episodes);

        if (episodes.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No episodes available for this anime.[/]");
            return;
        }

        var limitedEpisodes = episodes.Take(DisplayLimit).ToList();
        RenderAnimeEpisodeTable(limitedEpisodes, episodes.Count);

        var selectedEpisode = PromptChoice(
            "[bold]Select episode[/]",
            limitedEpisodes,
            episode => $"Episode {episode.Number:0.##} - {Escape(Clip(episode.Name, 45))}"
        );

        var servers = await RunWithStatusAsync(
            "[cyan]Loading video servers[/]",
            async () => await apiClient.GetVideoServersAsync(selectedEpisode.Id)
        );

        if (servers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No servers found for selected episode.[/]");
            return;
        }

        RenderVideoServerTable(servers);

        var selectedServer = PromptChoice(
            "[bold]Select server[/]",
            servers,
            server => $"{Escape(server.Name)} [grey]({Escape(Clip(server.Embed.Url, 55))})[/]"
        );

        var videoSources = await RunWithStatusAsync(
            "[cyan]Loading video sources[/]",
            async () => await apiClient.GetVideosAsync(selectedServer.Embed.Url)
        );

        RenderVideoSourceTable(videoSources);
        await TryDownloadVideoSourcesAsync(
            videoSources,
            $"{animeInfo.Title}-episode-{selectedEpisode.Number:0.##}"
        );
    }

    private static async Task RunVideoInspectionAsync(
        IAnimeProvider provider,
        string title,
        List<AnimeEpisode> episodes,
        Func<AnimeEpisode, string> episodeLabel
    )
    {
        if (episodes.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No episodes available for this anime.[/]");
            return;
        }

        var limitedEpisodes = episodes.Take(DisplayLimit).ToList();
        RenderAnimeEpisodeTable(limitedEpisodes, episodes.Count);

        var selectedEpisode = PromptChoice(
            "[bold]Select episode[/]",
            limitedEpisodes,
            episodeLabel
        );

        var servers = await RunWithStatusAsync(
            "[cyan]Loading video servers[/]",
            async () => await provider.GetVideoServersAsync(selectedEpisode.Id)
        );

        if (servers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No servers found for selected episode.[/]");
            return;
        }

        RenderVideoServerTable(servers);

        var selectedServer = PromptChoice(
            "[bold]Select server[/]",
            servers,
            server => $"{Escape(server.Name)} [grey]({Escape(Clip(server.Embed.Url, 55))})[/]"
        );

        var videoSources = await RunWithStatusAsync(
            "[cyan]Loading video sources[/]",
            async () => await provider.GetVideosAsync(selectedServer)
        );

        RenderVideoSourceTable(videoSources);
        await TryDownloadVideoSourcesAsync(
            videoSources,
            $"{title}-episode-{selectedEpisode.Number:0.##}"
        );
    }

    private static void RenderSourceProviderTable<TProvider>(
        string title,
        IReadOnlyList<TProvider> providers
    )
        where TProvider : ISourceProvider, IKey
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]{Escape(title)} ({providers.Count})[/]");

        table.AddColumn("#");
        table.AddColumn("Name");
        table.AddColumn("Key");
        table.AddColumn("Language");

        for (var i = 0; i < providers.Count; i++)
        {
            var provider = providers[i];
            table.AddRow(
                (i + 1).ToString(),
                Escape(provider.Name),
                Escape(provider.Key),
                Escape(provider.Language)
            );
        }

        AnsiConsole.Write(table);
    }

    private static void RenderAnimeSearchTable(
        IReadOnlyList<IAnimeInfo> results,
        int totalCount,
        string providerName
    )
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]{Escape(providerName)} anime results ({totalCount})[/]");

        table.AddColumn("#");
        table.AddColumn("Title");
        table.AddColumn("Type");
        table.AddColumn("Status");
        table.AddColumn("Id");

        for (var i = 0; i < results.Count; i++)
        {
            var anime = results[i];
            table.AddRow(
                (i + 1).ToString(),
                Escape(Clip(anime.Title, 38)),
                Escape(anime.Type),
                Escape(anime.Status),
                Escape(Clip(anime.Id, 24))
            );
        }

        AnsiConsole.Write(table);
    }

    private static void RenderAnimeDetails(
        string providerName,
        IAnimeInfo animeInfo,
        IReadOnlyCollection<AnimeEpisode> episodes
    )
    {
        var genres =
            animeInfo.Genres is not null && animeInfo.Genres.Count > 0
                ? string.Join(
                    ", ",
                    animeInfo
                        .Genres.Select(genre => genre.Name)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                )
                : "-";

        var details = new Table().Border(TableBorder.Rounded);
        details.AddColumn("Field");
        details.AddColumn("Value");
        details.AddRow("Provider", Escape(providerName));
        details.AddRow("Title", Escape(animeInfo.Title));
        details.AddRow("Id", Escape(animeInfo.Id));
        details.AddRow("Type", Escape(animeInfo.Type));
        details.AddRow("Status", Escape(animeInfo.Status));
        details.AddRow("Released", Escape(animeInfo.Released));
        details.AddRow("Episodes", episodes.Count.ToString());
        details.AddRow("Genres", Escape(Clip(genres, 80)));

        AnsiConsole.Write(
            new Panel(details).Header("[bold]Anime Details[/]").Border(BoxBorder.Rounded).Expand()
        );
    }

    private static void RenderAnimeEpisodeTable(
        IReadOnlyList<AnimeEpisode> episodes,
        int totalCount
    )
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]Episodes ({totalCount})[/]");

        table.AddColumn("#");
        table.AddColumn("Episode");
        table.AddColumn("Name");
        table.AddColumn("Id");

        for (var i = 0; i < episodes.Count; i++)
        {
            var episode = episodes[i];
            table.AddRow(
                (i + 1).ToString(),
                episode.Number.ToString("0.##"),
                Escape(Clip(episode.Name, 38)),
                Escape(Clip(episode.Id, 26))
            );
        }

        AnsiConsole.Write(table);
    }

    private static void RenderVideoServerTable(IReadOnlyList<VideoServer> servers)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]Video Servers ({servers.Count})[/]");

        table.AddColumn("#");
        table.AddColumn("Name");
        table.AddColumn("Url");

        for (var i = 0; i < servers.Count; i++)
        {
            var server = servers[i];
            table.AddRow(
                (i + 1).ToString(),
                Escape(server.Name),
                Escape(Clip(server.Embed.Url, 70))
            );
        }

        AnsiConsole.Write(table);
    }

    private static void RenderVideoSourceTable(IReadOnlyList<VideoSource> sources)
    {
        if (sources.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No video sources returned.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]Video Sources ({sources.Count})[/]");

        table.AddColumn("#");
        table.AddColumn("Label");
        table.AddColumn("Format");
        table.AddColumn("Subtitles");
        table.AddColumn("Url");

        for (var i = 0; i < sources.Count; i++)
        {
            var source = sources[i];
            var subtitleCount = source.Subtitles?.Count ?? 0;
            var label = string.IsNullOrWhiteSpace(source.Title) ? source.Resolution : source.Title;

            table.AddRow(
                (i + 1).ToString(),
                Escape(Clip(label, 30)),
                Escape(source.Format.ToString()),
                subtitleCount.ToString(),
                Escape(Clip(source.VideoUrl, 65))
            );
        }

        AnsiConsole.Write(table);
    }

    private static void RenderMangaSearchTable(
        IReadOnlyList<IMangaResult> results,
        int totalCount,
        string providerName
    )
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]{Escape(providerName)} manga results ({totalCount})[/]");

        table.AddColumn("#");
        table.AddColumn("Title");
        table.AddColumn("Id");

        for (var i = 0; i < results.Count; i++)
        {
            var manga = results[i];
            table.AddRow(
                (i + 1).ToString(),
                Escape(Clip(manga.Title, 44)),
                Escape(Clip(manga.Id, 32))
            );
        }

        AnsiConsole.Write(table);
    }

    private static void RenderMangaDetails(string providerName, IMangaInfo mangaInfo)
    {
        var genres = mangaInfo.Genres.Count > 0 ? string.Join(", ", mangaInfo.Genres) : "-";
        var authors = mangaInfo.Authors.Count > 0 ? string.Join(", ", mangaInfo.Authors) : "-";

        var details = new Table().Border(TableBorder.Rounded);
        details.AddColumn("Field");
        details.AddColumn("Value");
        details.AddRow("Provider", Escape(providerName));
        details.AddRow("Title", Escape(mangaInfo.Title));
        details.AddRow("Id", Escape(mangaInfo.Id));
        details.AddRow("Status", Escape(mangaInfo.Status.ToString()));
        details.AddRow("Genres", Escape(Clip(genres, 80)));
        details.AddRow("Authors", Escape(Clip(authors, 80)));
        details.AddRow("Chapters", mangaInfo.Chapters.Count.ToString());

        AnsiConsole.Write(
            new Panel(details).Header("[bold]Manga Details[/]").Border(BoxBorder.Rounded).Expand()
        );
    }

    private static void RenderMangaChapterTable(
        IReadOnlyList<IMangaChapter> chapters,
        int totalCount
    )
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]Chapters ({totalCount})[/]");

        table.AddColumn("#");
        table.AddColumn("Number");
        table.AddColumn("Title");
        table.AddColumn("Date");
        table.AddColumn("Id");

        for (var i = 0; i < chapters.Count; i++)
        {
            var chapter = chapters[i];
            table.AddRow(
                (i + 1).ToString(),
                chapter.Number.ToString("0.##"),
                Escape(Clip(chapter.Title, 34)),
                Escape(chapter.ReleasedDate),
                Escape(Clip(chapter.Id, 24))
            );
        }

        AnsiConsole.Write(table);
    }

    private static void RenderMangaPagesTable(IReadOnlyList<IMangaChapterPage> pages)
    {
        if (pages.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No chapter pages returned.[/]");
            return;
        }

        var limitedPages = pages.Take(DisplayLimit).ToList();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]Chapter Pages ({pages.Count})[/]");

        table.AddColumn("#");
        table.AddColumn("Page");
        table.AddColumn("Title");
        table.AddColumn("Image");

        for (var i = 0; i < limitedPages.Count; i++)
        {
            var page = limitedPages[i];
            table.AddRow(
                (i + 1).ToString(),
                page.Page.ToString(),
                Escape(Clip(page.Title, 35)),
                Escape(Clip(page.Image, 70))
            );
        }

        AnsiConsole.Write(table);
    }

    private static void RenderMovieSearchTable(
        IReadOnlyList<MovieResult> results,
        int totalCount,
        string providerName
    )
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]{Escape(providerName)} movie results ({totalCount})[/]");

        table.AddColumn("#");
        table.AddColumn("Title");
        table.AddColumn("Type");
        table.AddColumn("Released");
        table.AddColumn("Id");

        for (var i = 0; i < results.Count; i++)
        {
            var movie = results[i];
            table.AddRow(
                (i + 1).ToString(),
                Escape(Clip(movie.Title, 38)),
                Escape(movie.Type.ToString()),
                Escape(movie.ReleasedDate),
                Escape(Clip(movie.Id, 24))
            );
        }

        AnsiConsole.Write(table);
    }

    private static void RenderMovieDetails(string providerName, MovieInfo info)
    {
        var genres = info.Genres.Count > 0 ? string.Join(", ", info.Genres) : "-";

        var details = new Table().Border(TableBorder.Rounded);
        details.AddColumn("Field");
        details.AddColumn("Value");
        details.AddRow("Provider", Escape(providerName));
        details.AddRow("Title", Escape(info.Title));
        details.AddRow("Id", Escape(info.Id));
        details.AddRow("Type", Escape(info.Type.ToString()));
        details.AddRow("Released", Escape(info.ReleasedDate));
        details.AddRow("Duration", Escape(info.Duration));
        details.AddRow("Rating", Escape(info.Rating));
        details.AddRow("Genres", Escape(Clip(genres, 80)));
        details.AddRow("Episodes", info.Episodes.Count.ToString());

        AnsiConsole.Write(
            new Panel(details).Header("[bold]Movie Details[/]").Border(BoxBorder.Rounded).Expand()
        );
    }

    private static void RenderMovieEpisodeTable(
        IReadOnlyList<MovieEpisode> episodes,
        int totalCount
    )
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]Episodes ({totalCount})[/]");

        table.AddColumn("#");
        table.AddColumn("Season");
        table.AddColumn("Episode");
        table.AddColumn("Title");
        table.AddColumn("Id");

        for (var i = 0; i < episodes.Count; i++)
        {
            var episode = episodes[i];
            table.AddRow(
                (i + 1).ToString(),
                episode.Season.ToString(),
                episode.Number.ToString(),
                Escape(Clip(episode.Title, 34)),
                Escape(Clip(episode.Id, 24))
            );
        }

        AnsiConsole.Write(table);
    }

    private static void RenderApiProviderTable(IReadOnlyList<Provider> providers)
    {
        var table = new Table().Border(TableBorder.Rounded).Title("[bold]API Anime Providers[/]");
        table.AddColumn("#");
        table.AddColumn("Name");
        table.AddColumn("Key");
        table.AddColumn("Language");

        for (var i = 0; i < providers.Count; i++)
        {
            var provider = providers[i];
            table.AddRow(
                (i + 1).ToString(),
                Escape(provider.Name),
                Escape(provider.Key),
                Escape(provider.Language)
            );
        }

        AnsiConsole.Write(table);
    }

    private static async Task TryDownloadVideoSourcesAsync(
        IReadOnlyList<VideoSource> videoSources,
        string baseName
    )
    {
        if (videoSources.Count == 0)
        {
            return;
        }

        if (!AnsiConsole.Confirm("[grey]Download a video source?[/]", false))
        {
            return;
        }

        var selectedSource = PromptChoice(
            "[bold]Select video source[/]",
            videoSources,
            FormatVideoSourceLabel
        );

        if (IsDashSource(selectedSource))
        {
            AnsiConsole.MarkupLine(
                "[yellow]Dash manifest downloads are not wired into this demo yet.[/]"
            );
            return;
        }

        if (IsHlsSource(selectedSource))
        {
            var hlsDownloader = new HlsDownloader();
            var qualities = await RunWithStatusAsync(
                "[cyan]Inspecting HLS qualities[/]",
                async () =>
                    await hlsDownloader.GetQualitiesAsync(
                        selectedSource.VideoUrl,
                        selectedSource.Headers
                    )
            );

            if (qualities.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No HLS qualities were returned.[/]");
                return;
            }

            var selectedQuality =
                qualities.Count == 1
                    ? qualities[0]
                    : PromptChoice("[bold]Select HLS quality[/]", qualities, FormatQualityLabel);

            if (selectedQuality.Stream is null)
            {
                AnsiConsole.MarkupLine("[yellow]Selected quality does not expose a stream.[/]");
                return;
            }

            var outputPath = AskRequired(
                "[cyan]Output video path[/]",
                GetSuggestedVideoPath(
                    baseName,
                    selectedQuality.Resolution?.ToString() ?? selectedQuality.Name ?? "hls",
                    selectedQuality.OutputFormat.Extension
                )
            );

            outputPath = PrepareOutputFile(outputPath);

            await RunProgressAsync(
                "[cyan]Downloading HLS video[/]",
                async progress =>
                    await hlsDownloader.DownloadAllThenMergeAsync(
                        selectedQuality.Stream,
                        selectedSource.Headers,
                        outputPath,
                        progress
                    )
            );

            AnsiConsole.MarkupLine($"[green]Saved video to[/] [grey]{Escape(outputPath)}[/]");
            return;
        }

        var downloader = new Downloader();
        var videoPath = AskRequired(
            "[cyan]Output video path[/]",
            GetSuggestedVideoPath(
                baseName,
                selectedSource.Title
                    ?? selectedSource.Resolution
                    ?? selectedSource.Format.ToString(),
                GetUrlExtension(selectedSource.VideoUrl, ".mp4")
            )
        );

        videoPath = PrepareOutputFile(videoPath);

        await RunProgressAsync(
            "[cyan]Downloading video[/]",
            async progress =>
                await downloader.DownloadAsync(
                    selectedSource.VideoUrl,
                    videoPath,
                    selectedSource.Headers,
                    progress
                )
        );

        AnsiConsole.MarkupLine($"[green]Saved video to[/] [grey]{Escape(videoPath)}[/]");
    }

    private static async Task TryDownloadMangaPagesAsync(
        IReadOnlyList<IMangaChapterPage> pages,
        string baseName
    )
    {
        if (pages.Count == 0)
        {
            return;
        }

        if (!AnsiConsole.Confirm("[grey]Download manga pages?[/]", false))
        {
            return;
        }

        var mode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Choose download mode[/]")
                .AddChoices("Selected page", "All pages")
        );

        var downloader = new Downloader();

        if (mode == "Selected page")
        {
            var selectedPage = PromptChoice(
                "[bold]Select page[/]",
                pages.ToList(),
                page => $"Page {page.Page} [grey]({Escape(Clip(page.Image, 55))})[/]"
            );

            var pagePath = AskRequired(
                "[cyan]Output page path[/]",
                GetSuggestedPagePath(baseName, selectedPage)
            );

            pagePath = PrepareOutputFile(pagePath);

            await RunProgressAsync(
                "[cyan]Downloading page[/]",
                async progress =>
                    await downloader.DownloadAsync(
                        selectedPage.Image,
                        pagePath,
                        selectedPage.Headers,
                        progress
                    )
            );

            AnsiConsole.MarkupLine($"[green]Saved page to[/] [grey]{Escape(pagePath)}[/]");
            return;
        }

        var outputDirectory = AskRequired(
            "[cyan]Output folder[/]",
            Path.Combine(DownloadsDirectory, SanitizeFileName(baseName))
        );

        outputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(outputDirectory);

        await RunProgressAsync(
            "[cyan]Downloading manga pages[/]",
            async progress =>
            {
                for (var i = 0; i < pages.Count; i++)
                {
                    var page = pages[i];
                    var pagePath = Path.Combine(outputDirectory, GetPageFileName(page));
                    pagePath = PrepareOutputFile(pagePath);

                    await downloader.DownloadAsync(page.Image, pagePath, page.Headers);
                    progress.Report((i + 1) / (double)pages.Count);
                }
            }
        );

        AnsiConsole.MarkupLine($"[green]Saved pages to[/] [grey]{Escape(outputDirectory)}[/]");
    }

    private static async Task<T> RunWithStatusAsync<T>(string status, Func<Task<T>> action)
    {
        T? result = default;

        await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(status, async _ => result = await action());

        return result!;
    }

    private static async Task RunProgressAsync(
        string description,
        Func<IProgress<double>, Task> action
    )
    {
        await AnsiConsole
            .Progress()
            .StartAsync(async ctx =>
            {
                var progressTask = ctx.AddTask(description);
                progressTask.MaxValue = 100;

                var progress = new Progress<double>(value =>
                {
                    progressTask.Value = Math.Clamp(value * 100.0, 0, 100);
                });

                await action(progress);
                progressTask.Value = 100;
            });
    }

    private static List<TProvider> OrderProviders<TProvider>(
        IEnumerable<TProvider> providers,
        string preferredKey
    )
        where TProvider : ISourceProvider, IKey =>
        providers
            .OrderBy(provider =>
                !string.Equals(provider.Key, preferredKey, StringComparison.OrdinalIgnoreCase)
            )
            .ThenBy(provider => provider.Name)
            .ToList();

    private static string FormatProviderLabel<TProvider>(TProvider provider)
        where TProvider : ISourceProvider, IKey =>
        $"{Escape(provider.Name)} [grey]({Escape(provider.Key)} | {Escape(provider.Language)})[/]";

    private static string FormatVideoSourceLabel(VideoSource source)
    {
        var label = source.Title ?? source.Resolution ?? source.Format.ToString();
        return $"{Escape(Clip(label, 42))} [grey]({Escape(source.Format.ToString())})[/]";
    }

    private static string FormatQualityLabel(HlsStreamMetadata quality)
    {
        var label = quality.Resolution?.ToString() ?? quality.Name ?? "HLS";
        var bandwidth = quality.Bandwidth > 0 ? $"{quality.Bandwidth / 1000} kbps" : "unknown";
        return $"{Escape(label)} [grey]({Escape(bandwidth)})[/]";
    }

    private static bool IsHlsSource(VideoSource source) =>
        source.Format is VideoType.Hls or VideoType.M3u8
        || source.VideoUrl.Contains(".m3u8", StringComparison.OrdinalIgnoreCase);

    private static bool IsDashSource(VideoSource source) =>
        source.Format == VideoType.Dash
        || source.VideoUrl.Contains(".mpd", StringComparison.OrdinalIgnoreCase);

    private static string GetSuggestedVideoPath(
        string baseName,
        string variant,
        string extension
    ) =>
        Path.Combine(
            DownloadsDirectory,
            $"{SanitizeFileName(baseName)}-{SanitizeFileName(variant)}{EnsureExtension(extension)}"
        );

    private static string GetSuggestedPagePath(string baseName, IMangaChapterPage page) =>
        Path.Combine(DownloadsDirectory, SanitizeFileName(baseName), GetPageFileName(page));

    private static string GetPageFileName(IMangaChapterPage page) =>
        $"{page.Page:D3}{GetUrlExtension(page.Image, ".jpg")}";

    private static string GetUrlExtension(string? url, string fallback)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return EnsureExtension(fallback);
        }

        var extension = Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? Path.GetExtension(uri.LocalPath)
            : Path.GetExtension(url);

        if (string.IsNullOrWhiteSpace(extension) || extension.Length > 10)
        {
            return EnsureExtension(fallback);
        }

        return EnsureExtension(extension);
    }

    private static string EnsureExtension(string extension) =>
        extension.StartsWith('.') ? extension : $".{extension}";

    private static string PrepareOutputFile(string outputPath)
    {
        outputPath = Path.GetFullPath(outputPath);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        return outputPath;
    }

    private static string SanitizeFileName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "download";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(
            value.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray()
        );

        sanitized = sanitized.Trim().Trim('.');

        return string.IsNullOrWhiteSpace(sanitized) ? "download" : sanitized;
    }

    private static T PromptChoice<T>(string title, IReadOnlyList<T> options, Func<T, string> label)
        where T : notnull =>
        AnsiConsole.Prompt(
            new SelectionPrompt<T>()
                .Title(title)
                .PageSize(10)
                .UseConverter(label)
                .AddChoices(options)
        );

    private static string AskRequired(string prompt, string defaultValue) =>
        AnsiConsole.Prompt(
            new TextPrompt<string>(prompt)
                .PromptStyle("cyan")
                .DefaultValue(defaultValue)
                .ShowDefaultValue()
                .Validate(value =>
                    string.IsNullOrWhiteSpace(value)
                        ? ValidationResult.Error("[red]Value is required.[/]")
                        : ValidationResult.Success()
                )
        );

    private static string Escape(string? value) =>
        Markup.Escape(string.IsNullOrWhiteSpace(value) ? "-" : value);

    private static string Clip(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        var normalized = value.Replace('\r', ' ').Replace('\n', ' ');
        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        return normalized[..(maxLength - 3)] + "...";
    }
}
