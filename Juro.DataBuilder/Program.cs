using System.Text.Json;
using Httpz;
using Juro.Core.Models.Anime;
using Juro.Core.Providers;
using Juro.Core.Utils.Tasks;
using Juro.DataBuilder.Models;
using Juro.Providers.Anime;
using Juro.Providers.Anime.Indonesian;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace Juro.DataBuilder;

internal class Program
{
    static async Task Main()
    {
        if (!Directory.Exists("output"))
            Directory.CreateDirectory("output");

        var filePath = Path.Combine("output", "anime-offline-database-minified.json");
        if (!File.Exists(filePath))
            await DownloadManamiDbAsync(filePath);

        var dbPath = Path.Join(Environment.CurrentDirectory, "output", "juro.db");

        using var db = new JuroContext(dbPath);

        if (!File.Exists(dbPath))
        {
            AnsiConsole.MarkupLine($"[cyan]Reading json file '{filePath}'[/]");
            AnsiConsole.WriteLine();

            var data = File.ReadAllText(filePath);

            var root = JsonSerializer.Deserialize<Root>(data);

            ArgumentNullException.ThrowIfNull(root);

            AnsiConsole.MarkupLine("[cyan]Creating database[/]");

            await db.Database.EnsureCreatedAsync();

            await db.AnimeItems.AddRangeAsync(root.Data);
            await db.SaveChangesAsync();

            AnsiConsole.WriteLine($"Database saved to '{dbPath}'");
            AnsiConsole.WriteLine();
        }

        AnsiConsole.MarkupLine("[cyan]Matching all anime. This may take a while[/]");

        var animes = await db.AnimeItems.ToListAsync();

        //var test1 = animes.Where(x => x.GogoanimeId is null).ToList();
        //var test2 = animes.Where(x => x.GogoanimeId is null && x.AnimePaheId is null).ToList();
        //
        //var test3 = animes.Where(x => x.GogoanimeId is not null).ToList();

        //foreach (var item in root.Data)
        //{
        //    await TryLinkAnimeToProviderAsync(item);
        //}

        var functions = animes.Select(x =>
            (Func<Task>)(async () => await TrySetProvidersForAnimeAsync(x))
        );

        await AnsiConsole
            .Progress()
            .StartAsync(async ctx =>
            {
                var progressTask = ctx.AddTask($"[cyan]Linking[/]");
                progressTask.MaxValue = animes.Count;

                await TaskEx.Run(functions, 200, progressTask);

                progressTask.Value = animes.Count;
            });

        await db.SaveChangesAsync();

        AnsiConsole.MarkupLine("[cyan]Completed[/]");
        AnsiConsole.WriteLine();

        Console.ReadLine();
    }

    private static async Task DownloadManamiDbAsync(string filePath)
    {
        await AnsiConsole
            .Progress()
            .StartAsync(async ctx =>
            {
                var progressTask = ctx.AddTask(
                    $"[cyan]Downloading {Path.GetFileName(filePath)}[/]"
                );
                progressTask.MaxValue = 1;

                var downloader = new Downloader();
                await downloader.DownloadAsync(
                    "https://github.com/manami-project/anime-offline-database/raw/master/anime-offline-database-minified.json",
                    filePath,
                    progress: progressTask
                );
            });
    }

    private static async Task TrySetProvidersForAnimeAsync(ManamiAnimeItem anime)
    {
        var result = await TryFindBestAnime(Providers.Gogoanime, anime);
        if (result is not null)
        {
            anime.GogoanimeId = result.Id;
        }

        result = await TryFindBestAnime(Providers.AnimePahe, anime);
        if (result is not null)
        {
            anime.AnimePaheId = result.Id;
        }

        result = await TryFindBestAnime(Providers.Kaido, anime);
        if (result is not null)
        {
            anime.KaidoId = result.Id;
        }

        //result = await TryFindBestAnime(Providers.Aniwave, anime);
        //if (result is not null)
        //{
        //    anime.AniwaveId = result.Id;
        //}
        //
        //result = await TryFindBestAnime(Providers.OtakuDesu, anime);
        //if (result is not null)
        //{
        //    anime.OtakuDesuId = result.Id;
        //}
    }

    private static async Task<IAnimeInfo?> TryFindBestAnime(
        IAnimeProvider provider,
        ManamiAnimeItem entity
    )
    {
        var titles = new List<string> { entity.Title };
        titles.AddRange(entity.Synonyms);

        try
        {
            foreach (var title in titles)
            {
                var result = await provider.SearchAsync(entity.Title);

                if (result.Count > 0)
                {
                    return result.FirstOrDefault();
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}

public static class Providers
{
#pragma warning disable CS0618
    public static Gogoanime Gogoanime { get; set; } = new();
#pragma warning restore CS0618
    public static AnimePahe AnimePahe { get; set; } = new();
    public static Kaido Kaido { get; set; } = new();
    public static Aniwave Aniwave { get; set; } = new();
    public static OtakuDesu OtakuDesu { get; set; } = new();
}
