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

    public IClientConfig? GetClientConfig(Module module) => GetClientConfig(module.FilePath);

    /// <summary>
    /// Gets an instance of the first class in the assembly that implements
    /// the interface <see cref="IClientConfig"/>.
    /// </summary>
    /// <param name="filePath">The file path to the assembly (dll) that contains
    /// the interface <see cref="IClientConfig"/>.</param>
    public IClientConfig? GetClientConfig(string filePath)
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
                .Select(x => (IClientConfig?)Activator.CreateInstance(x, new object[] { }))
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public List<IClientConfig> GetClientConfigs()
    {
        var list = new List<IClientConfig>();

        try
        {
            foreach (var assembly in GetAssemblies())
            {
                var config = assembly
                    .GetTypes()
                    .Where(
                        x =>
                            x.GetInterfaces().Contains(typeof(IClientConfig))
                            && x.GetConstructor(Type.EmptyTypes) is not null
                    )
                    .Select(x => (IClientConfig?)Activator.CreateInstance(x, new object[] { }))
                    .FirstOrDefault();

                if (config is not null)
                    list.Add(config);
            }
        }
        catch { }

        return list;
    }

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
        var filePaths = Directory.EnumerateFiles(
            dirPath,
            SearchPattern,
            SearchOption.AllDirectories
        );

        foreach (var filePath in filePaths)
            LoadFile(filePath);
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
}
