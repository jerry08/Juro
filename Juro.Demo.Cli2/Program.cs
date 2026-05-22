using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Juro.Clients;
using Juro.Core;
using Juro.Core.Models.Anime;
using Juro.Core.Providers;
using Spectre.Console;

namespace Juro.Demo.Cli2;

internal static class Program
{
    private const int DisplayLimit = 20;

    static async Task Main()
    {
        Console.Title = "Juro Plugin Demo";

        AnsiConsole.Write(new FigletText("Plugin Demo").Color(Color.Green));
        AnsiConsole.MarkupLine(
            "[grey]Loads provider assemblies and explores discovered anime providers.[/]"
        );

        var defaultPluginPath = Path.GetFullPath(
            Path.Combine(
                Environment.CurrentDirectory,
                "..",
                "..",
                "..",
                "..",
                "Juro.Providers",
                "bin",
                "Debug",
                "net10.0"
            )
        );

        var pluginPath = AnsiConsole.Prompt(
            new TextPrompt<string>("[cyan]Plugin directory[/]")
                .PromptStyle("cyan")
                .DefaultValue(defaultPluginPath)
                .ShowDefaultValue()
                .Validate(path =>
                    Directory.Exists(path)
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]Directory does not exist.[/]")
                )
        );

        await LoadAndExplorePluginsAsync(pluginPath);
    }

    private static async Task LoadAndExplorePluginsAsync(string pluginPath)
    {
        var loadedAssemblies = await RunWithStatusAsync(
            "[cyan]Loading plugins[/]",
            async () =>
            {
                await Task.Yield();
                return PluginLoader.LoadPlugins(pluginPath);
            }
        );

        var plugins = PluginLoader.GetPlugins();
        var configs = PluginLoader.GetClientConfigs().ToList();
        var client = new AnimeClient();
        var providers = client.GetAllProviders().OrderBy(provider => provider.Name).ToList();

        RenderPluginSummary(pluginPath, loadedAssemblies.Count, plugins.Count, providers.Count);
        RenderPluginTable(plugins);
        RenderConfigTable(configs);

        if (providers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No anime providers discovered from loaded plugins.[/]");
            return;
        }

        RenderProviderTable(providers);

        var selectedProvider = AnsiConsole.Prompt(
            new SelectionPrompt<IAnimeProvider>()
                .Title("[bold]Select provider[/]")
                .PageSize(10)
                .UseConverter(provider =>
                    $"{Escape(provider.Name)} [grey]({Escape(provider.Key)} | {Escape(provider.Language)})[/]"
                )
                .AddChoices(providers)
        );

        var query = AnsiConsole.Prompt(
            new TextPrompt<string>("[cyan]Anime search query[/]")
                .PromptStyle("cyan")
                .DefaultValue("naruto")
                .ShowDefaultValue()
                .Validate(value =>
                    string.IsNullOrWhiteSpace(value)
                        ? ValidationResult.Error("[red]Value is required.[/]")
                        : ValidationResult.Success()
                )
        );

        var searchResults = await RunWithStatusAsync(
            $"[cyan]Searching {Escape(selectedProvider.Name)}[/]",
            async () => await selectedProvider.SearchAsync(query)
        );

        if (searchResults.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No results found for selected provider.[/]");
            return;
        }

        var limitedResults = searchResults.Take(DisplayLimit).ToList();
        RenderSearchTable(limitedResults, searchResults.Count);

        var selectedAnime = AnsiConsole.Prompt(
            new SelectionPrompt<IAnimeInfo>()
                .Title("[bold]Select anime[/]")
                .PageSize(10)
                .UseConverter(anime =>
                    $"{Escape(Clip(anime.Title, 52))} [grey]({Escape(Clip(anime.Id, 30))})[/]"
                )
                .AddChoices(limitedResults)
        );

        var animeInfo = await RunWithStatusAsync(
            "[cyan]Loading anime details[/]",
            async () => await selectedProvider.GetAnimeInfoAsync(selectedAnime.Id)
        );
        var episodes = await RunWithStatusAsync(
            "[cyan]Loading episodes[/]",
            async () => await selectedProvider.GetEpisodesAsync(selectedAnime.Id)
        );

        RenderAnimeInfoPanel(selectedProvider, animeInfo, episodes.Count);
        RenderEpisodeTable(episodes.Take(DisplayLimit).ToList(), episodes.Count);
    }

    private static void RenderPluginSummary(
        string pluginPath,
        int assemblyCount,
        int pluginCount,
        int providerCount
    )
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow("Path", Escape(pluginPath));
        grid.AddRow("Assemblies", assemblyCount.ToString());
        grid.AddRow("Plugins", pluginCount.ToString());
        grid.AddRow("Anime Providers", providerCount.ToString());

        AnsiConsole.Write(
            new Panel(grid).Header("[bold]Plugin Summary[/]").Border(BoxBorder.Rounded).Expand()
        );
    }

    private static void RenderPluginTable(IReadOnlyList<Plugin> plugins)
    {
        var table = new Table().Border(TableBorder.Rounded).Title("[bold]Loaded Plugins[/]");
        table.AddColumn("Name");
        table.AddColumn("Version");
        table.AddColumn("Repository");
        table.AddColumn("Path");

        foreach (var plugin in plugins)
        {
            table.AddRow(
                Escape(plugin.Name),
                Escape(plugin.Version?.ToString()),
                Escape(plugin.ClientConfig?.RepositoryUrl),
                Escape(Clip(plugin.FilePath, 60))
            );
        }

        AnsiConsole.Write(table);
    }

    private static void RenderConfigTable(IReadOnlyList<IClientConfig> configs)
    {
        if (configs.Count == 0)
        {
            return;
        }

        var table = new Table().Border(TableBorder.Rounded).Title("[bold]Client Configs[/]");
        table.AddColumn("Repository");
        table.AddColumn("Minimum Supported Version");

        foreach (var config in configs)
        {
            table.AddRow(
                Escape(config.RepositoryUrl),
                Escape(config.MinimumSupportedVersion.ToString())
            );
        }

        AnsiConsole.Write(table);
    }

    private static void RenderProviderTable(IReadOnlyList<IAnimeProvider> providers)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Discovered Anime Providers[/]");
        table.AddColumn("#");
        table.AddColumn("Name");
        table.AddColumn("Key");
        table.AddColumn("Language");
        table.AddColumn("Dub Split");

        for (var i = 0; i < providers.Count; i++)
        {
            var provider = providers[i];
            table.AddRow(
                (i + 1).ToString(),
                Escape(provider.Name),
                Escape(provider.Key),
                Escape(provider.Language),
                provider.IsDubAvailableSeparately ? "Yes" : "No"
            );
        }

        AnsiConsole.Write(table);
    }

    private static void RenderSearchTable(IReadOnlyList<IAnimeInfo> results, int totalCount)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]Search Results ({totalCount})[/]");
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

    private static void RenderAnimeInfoPanel(
        IAnimeProvider provider,
        IAnimeInfo animeInfo,
        int episodeCount
    )
    {
        var genres =
            animeInfo.Genres.Count > 0
                ? string.Join(", ", animeInfo.Genres.Select(genre => genre.Name))
                : "-";

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Field");
        table.AddColumn("Value");
        table.AddRow("Provider", Escape(provider.Name));
        table.AddRow("Key", Escape(provider.Key));
        table.AddRow("Title", Escape(animeInfo.Title));
        table.AddRow("Status", Escape(animeInfo.Status));
        table.AddRow("Type", Escape(animeInfo.Type));
        table.AddRow("Genres", Escape(Clip(genres, 80)));
        table.AddRow("Episodes", episodeCount.ToString());

        AnsiConsole.Write(
            new Panel(table).Header("[bold]Anime Details[/]").Border(BoxBorder.Rounded).Expand()
        );
    }

    private static void RenderEpisodeTable(IReadOnlyList<Episode> episodes, int totalCount)
    {
        if (episodes.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No episodes returned.[/]");
            return;
        }

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
                Escape(Clip(episode.Id, 28))
            );
        }

        AnsiConsole.Write(table);
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
