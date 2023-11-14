using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Juro.Core;
using Juro.Core.Attributes;

namespace Juro;

public class Locator
{
    internal static Locator Instance => new();

    /// <summary>
    /// Search for files/modules with a pattern.
    /// </summary>
    /// <value>*.dll</value>
    public string SearchPattern { get; set; } = "*.dll";

    /// <summary>
    /// Initializes an instance of <see cref="Locator"/>.
    /// </summary>
    public Locator() { }

    /// <inheritdoc cref="GetClientConfig(Module)" />
    public IClientConfig? TryGetClientConfig(Module module)
    {
        try
        {
            return GetClientConfig(module.FilePath);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets an instance of the first class in the assembly that implements
    /// the interface <see cref="IClientConfig"/>.
    /// </summary>
    /// <param name="module">The module that contains information about the assembly (dll)
    /// which contains the interface <see cref="IClientConfig"/>.</param>
    public IClientConfig GetClientConfig(Module module) => GetClientConfig(module.FilePath);

    /// <inheritdoc cref="GetClientConfig(string)" />
    public IClientConfig? TryGetClientConfig(string filePath)
    {
        try
        {
            return GetClientConfig(filePath);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets an instance of the first class in the assembly that implements
    /// the interface <see cref="IClientConfig"/>.
    /// </summary>
    /// <param name="filePath">The file path to the assembly (dll) that contains
    /// the interface <see cref="IClientConfig"/>.</param>
    public IClientConfig GetClientConfig(string filePath)
    {
        var loadedAssemblies = GetAssemblies();

        var assembly =
            loadedAssemblies.Find(
                x => string.Equals(x.Location, filePath, StringComparison.OrdinalIgnoreCase)
            ) ?? Assembly.LoadFile(filePath);

        return assembly
            .GetTypes()
            .Where(
                x =>
                    x.GetInterfaces().Contains(typeof(IClientConfig))
                    && x.GetConstructor(Type.EmptyTypes) is not null
            )
            .Select(x => (IClientConfig)Activator.CreateInstance(x, Array.Empty<object>())!)
            .FirstOrDefault()!;
    }

    /// <inheritdoc cref="GetClientConfigs" />
    public List<IClientConfig> TryGetClientConfigs()
    {
        try
        {
            return GetClientConfigs();
        }
        catch
        {
            return new();
        }
    }

    /// <summary>
    /// Gets all instances of <see cref="IClientConfig"/> in the Current
    /// App Domain (<see cref="AppDomain.CurrentDomain"/>).
    /// </summary>
    public List<IClientConfig> GetClientConfigs()
    {
        var list = new List<IClientConfig>();

        foreach (var assembly in GetAssemblies())
        {
            var config = assembly
                .GetTypes()
                .Where(
                    x =>
                        x.GetInterfaces().Contains(typeof(IClientConfig))
                        && x.GetConstructor(Type.EmptyTypes) is not null
                )
                .Select(x => (IClientConfig)Activator.CreateInstance(x, Array.Empty<object>())!)
                .FirstOrDefault();

            if (config is not null)
                list.Add(config);
        }

        return list;
    }

    /// <summary>
    /// Gets all loaded assemblies in the Current App Domain
    /// (<see cref="AppDomain.CurrentDomain"/>).
    /// </summary>
    public List<Assembly> GetAssemblies() =>
        AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(x => x.GetCustomAttributes(typeof(ModuleAssemblyAttribute), false).Length > 0)
            .ToList();

    /// <inheritdoc cref="GetModules" />
    public List<Module> TryGetModules()
    {
        try
        {
            return GetModules();
        }
        catch
        {
            return new();
        }
    }

    /// <summary>
    /// Gets all loaded modules in the Current App Domain
    /// (<see cref="AppDomain.CurrentDomain"/>).
    /// </summary>
    public List<Module> GetModules()
    {
        var list = new List<Module>();

        list.AddRange(
            GetAssemblies()
                .Select(assembly => new Module(assembly.GetName().Name, assembly.Location))
        );

        if (list.Count > 0)
        {
            var clientConfig = GetClientConfig(list[0].FilePath);
            list.ForEach(x => x.ClientConfig = clientConfig);
        }

        return list;
    }

    /// <summary>
    /// Loads all modules given its directory.
    /// </summary>
    /// <param name="dirPath">the directory where the modules can be found.</param>
    public void Load(string dirPath)
    {
        var filePaths = Directory.EnumerateFiles(
            dirPath,
            SearchPattern,
            SearchOption.AllDirectories
        );

        foreach (var filePath in filePaths)
            LoadFile(filePath);
    }

    /// <summary>
    /// Loads a module given its file name or path.
    /// </summary>
    /// <param name="filePath">The file path of the module.</param>
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
}
