using System;
using System.Linq;
using System.Threading.Tasks;
using Juro.Clients;

namespace Juro.Demo.Cli2;

interface ITest1 { }

interface ITest2 { }

interface ITest3 : ITest2 { }

class Test1 : ITest1 { }

class Test2 : ITest2 { }

class Test3 : ITest3 { }

internal static class Program
{
    static async Task Main()
    {
        //var test1 = (ITest2?)((ITest1)new Test1());
        var test2 = (ITest3?)((ITest2)new Test3());

        var client = new AnimeClient();

        // At this point, the list will be empty because no assemblies/plugins
        // with classes that implement `IAnimeProvider` interface are loaded.
        var providers = client.GetAllProviders();

        //var dir1 = Environment.CurrentDirectory;
        var dirPath2 = @"..\..\..\..\Juro.Providers\bin\Debug\net6.0";
        //var dirInfo = new DirectoryInfo(Environment.CurrentDirectory);
        //var parent1 = dirInfo.Parent;
        //var parent2 = dirInfo.Parent?.Parent;

        //var dirInfo2 = new DirectoryInfo(dirPath2);

        var loadedPlugins = PluginLoader.LoadPlugins(dirPath2);
        var plugins = PluginLoader.GetPlugins();
        var configs = PluginLoader.GetClientConfigs().ToList();

        foreach (var plugin in plugins)
        {
            Console.WriteLine($"{plugin.ClientConfig?.RepositoryUrl}");
            Console.WriteLine($"{plugin.Name} ({plugin.Version}) loaded from '${plugin.FilePath}'");
        }

        foreach (var config in configs)
        {
            Console.WriteLine($"{config.RepositoryUrl}");
        }

        // At this point, the list will be populated now that assemblies are loaded
        providers = client.GetAllProviders();

        var selectedProvider = providers[3];

        Console.WriteLine($"Searching {selectedProvider.Name}...");

        var searchResult = await selectedProvider.SearchAsync("naruto");
        Console.WriteLine($"Search count: {searchResult.Count}");

        Console.WriteLine("Getting episodes...");

        var episodes = await selectedProvider.GetEpisodesAsync(searchResult[0].Id);
        Console.WriteLine($"Episodes count: {episodes.Count}");

        Console.Read();
    }
}
