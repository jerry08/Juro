using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Juro.Core;
using Juro.Core.Attributes;
using Juro.Core.Utils;

namespace Juro;

public class ModuleProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    internal static ModuleProvider Instance => new();

    /// <summary>
    /// Search for files/modules with a pattern.
    /// </summary>
    /// <value>*.dll</value>
    public string SearchPattern { get; set; } = "*.dll";

    /// <summary>
    /// Initializes an instance of <see cref="ModuleProvider"/>.
    /// </summary>
    public ModuleProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Initializes an instance of <see cref="ModuleProvider"/>.
    /// </summary>
    public ModuleProvider(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="ModuleProvider"/>.
    /// </summary>
    public ModuleProvider()
        : this(Http.ClientProvider) { }

    private IClientConfig? GetClientConfig(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            return Assembly
                .LoadFile(filePath)
                .GetTypes()
                .Where(
                    x =>
                        x.GetInterfaces().Contains(typeof(IClientConfig))
                        && x.GetConstructor(Type.EmptyTypes) is not null
                )
                .Select(x => (IClientConfig)Activator.CreateInstance(x, new object[] { })!)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /*public List<Module> GetModules(string dirPath)
    {
        var list = new List<Module>();

        var filePaths = Directory
            .EnumerateFiles(dirPath, "*.dll", SearchOption.AllDirectories);

        foreach (var filePath in filePaths)
        {
            //var assemblyInfo = AssemblyName.GetAssemblyName(filePath);
            var test = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
        }

        return list;
    }*/

    public List<Assembly> GetAssemblies() =>
        AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(x => x.GetCustomAttributes(typeof(ModuleAssemblyAttribute), false).Length > 0)
            .ToList();

    public List<Module> GetModules()
    {
        var list = new List<Module>();

        try
        {
            list.AddRange(
                GetAssemblies()
                    .Select(assembly => new Module(assembly.GetName().Name, assembly.Location))
            );
        }
        catch { }

        return list;
    }

    public void Load(string dirPath)
    {
        //var tt = AssemblyEx.GetReferencedAssemblies().ToList();
        //
        //var test1 = AppDomain.CurrentDomain.GetAssemblies();

        var filePaths = Directory.EnumerateFiles(dirPath, SearchPattern, SearchOption.AllDirectories);

        foreach (var filePath in filePaths)
            LoadFile(filePath);

        //var tt2 = AssemblyEx.GetReferencedAssemblies().ToList();
        //
        //var test3 = AppDomain.CurrentDomain.GetAssemblies();
    }

    public bool LoadFile(string filePath)
    {
        try
        {
            // Load all dependencies that resides in the root directory
            Assembly.LoadFrom(filePath);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task LoadFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            await DownloadAsync(filePath);

        var assembly = Assembly.Load(File.ReadAllBytes(filePath));
    }

    public async Task DownloadAsync(string filePath)
    {
        await Task.Delay(1);
    }
}
