using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Juro.Core;
using Juro.Core.Attributes;
using Juro.Utils;

namespace Juro;

public static class PluginLoader
{
    /// <inheritdoc cref="GetClientConfig(Plugin)" />
    public static IClientConfig? TryGetClientConfig(Plugin plugin)
    {
        try
        {
            return GetClientConfig(plugin.FilePath);
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
    /// <param name="plugin">The plugin that contains information about the assembly (dll)
    /// which contains the interface <see cref="IClientConfig"/>.</param>
    public static IClientConfig GetClientConfig(Plugin plugin) => GetClientConfig(plugin.FilePath);

    /// <inheritdoc cref="GetClientConfig(string)" />
    public static IClientConfig? TryGetClientConfig(string filePath)
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
    public static IClientConfig GetClientConfig(string filePath)
    {
        var loadedAssemblies = GetAssemblies();

        var assembly =
            loadedAssemblies.Find(
                x => string.Equals(x.Location, filePath, StringComparison.OrdinalIgnoreCase)
            ) ?? LoadPlugin(filePath);

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
    public static List<IClientConfig> TryGetClientConfigs()
    {
        try
        {
            return GetClientConfigs().ToList();
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
    public static IEnumerable<IClientConfig> GetClientConfigs()
    {
        foreach (var assembly in GetAssemblies())
        {
            foreach (var config in CreateClientConfigs(assembly))
            {
                yield return config;
            }
        }
    }

    private static IEnumerable<IClientConfig> CreateClientConfigs(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (typeof(IClientConfig).IsAssignableFrom(type))
            {
                var result = (IClientConfig?)Activator.CreateInstance(type);
                if (result is not null)
                {
                    yield return result;
                }
            }
        }
    }

    /// <summary>
    /// Gets all loaded assemblies in the Current App Domain
    /// (<see cref="AppDomain.CurrentDomain"/>).
    /// </summary>
    public static List<Assembly> GetAssemblies() =>
        AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(x => x.GetCustomAttributes(typeof(PluginAssemblyAttribute), false).Length > 0)
            //.Where(
            //    x =>
            //        x.CustomAttributes.Any(
            //            y => y.AttributeType?.FullName == typeof(PluginAssemblyAttribute)?.FullName
            //        )
            //)
            .ToList();

    /// <inheritdoc cref="GetPlugins" />
    public static List<Plugin> TryGetPlugins()
    {
        try
        {
            return GetPlugins();
        }
        catch
        {
            return new();
        }
    }

    /// <summary>
    /// Gets all loaded plugins in the Current App Domain
    /// (<see cref="AppDomain.CurrentDomain"/>).
    /// </summary>
    public static List<Plugin> GetPlugins()
    {
        var list = new List<Plugin>();

        list.AddRange(
            GetAssemblies()
                .Select(assembly => new Plugin(assembly.GetName().Name, assembly.Location))
        );

        if (list.Count > 0)
        {
            var clientConfig = GetClientConfig(list[0].FilePath);
            list.ForEach(x => x.ClientConfig = clientConfig);
        }

        return list;
    }

    /// <summary>
    /// Loads all plugins given its directory.
    /// </summary>
    /// <param name="pluginsDir">the directory where the plugins can be found.</param>
    /// <param name="searchPattern">Search for files/plugins with a pattern.</param>
    /// <returns></returns>
    public static List<Assembly> LoadPlugins(string pluginsDir, string searchPattern = "*.dll")
    {
        var filePaths = Directory.EnumerateFiles(
            pluginsDir,
            searchPattern,
            SearchOption.AllDirectories
        );

        var list = new List<Assembly>();

        foreach (var filePath in filePaths)
            list.Add(LoadPlugin(filePath));

        return list;
    }

    /// <summary>
    /// Loads a plugin given its file name or path.
    /// </summary>
    /// <param name="pluginLocation">The file path of the plugin.</param>
    public static Assembly LoadPlugin(string pluginLocation)
    {
        if (OperatingSystem.IsAndroid())
            return Assembly.LoadFrom(pluginLocation);

        Debug.WriteLine($"Loading commands from: {pluginLocation}");
        var loadContext = new PluginLoadContext(pluginLocation);
        return loadContext.LoadFromAssemblyName(AssemblyName.GetAssemblyName(pluginLocation));
    }
}
