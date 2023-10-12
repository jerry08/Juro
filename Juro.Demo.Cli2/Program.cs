using System;
using System.IO;
using System.Threading.Tasks;
using Juro.Clients;

namespace Juro.Demo.Cli2;

interface ITest1
{
}

interface ITest2
{
}

interface ITest3 : ITest2
{
}

class Test1 : ITest1
{
}

class Test2 : ITest2
{
}

class Test3 : ITest3
{
}

internal static class Program
{
    static async Task Main()
    {
        //var test1 = (ITest2?)((ITest1)new Test1());
        var test2 = (ITest3?)((ITest2)new Test3());

        var client = new AnimeClient();

        // At this point, the list will be empty because no assemblies with classes that
        // implement `IAnimeProvider` interface providers are loaded
        var providers = client.GetAllProviders();

        //var dir1 = Environment.CurrentDirectory;
        var dirPath2 = @"..\..\..\..\Juro.Providers\bin\Debug\net6.0";
        //var dirInfo = new DirectoryInfo(Environment.CurrentDirectory);
        //var parent1 = dirInfo.Parent;
        //var parent2 = dirInfo.Parent?.Parent;

        //var dirInfo2 = new DirectoryInfo(dirPath2);

        var moduleProvider = new ModuleProvider();
        moduleProvider.Load(dirPath2);
        var modules = moduleProvider.GetModules();

        foreach (var module in modules)
        {
            Console.WriteLine($"{module.Name} ({module.Version}) loaded from '${module.FilePath}'");
        }

        // At this point, the list will be populated now that assemblies are loaded
        providers = client.GetAllProviders();

        var searchResult = await providers[0].SearchAsync("naruto");
    }
}