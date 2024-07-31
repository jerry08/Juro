using System;
using System.Collections.Generic;
using System.Linq;
using Juro.Core;
using Juro.Core.Providers;

namespace Juro.Clients;

public class ClientBase<IProvider>(IHttpClientFactory httpClientFactory) : IJuroClient<IProvider>
    where IProvider : ISourceProvider
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public IList<IProvider> GetAllProviders() => GetProviders();

    //public IList<IProvider> GetProviders(string? language = null)
    //    => Assembly.GetExecutingAssembly().GetTypes()
    //        .Where(x => x.GetInterfaces().Contains(typeof(IProvider))
    //            && x.GetConstructor(Type.EmptyTypes) is not null)
    //        .Select(x => (IProvider)Activator.CreateInstance(x, new object[] { _httpClientFactory })!)
    //        .Where(x => string.IsNullOrEmpty(language)
    //            || x.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
    //        .ToList();

    public IList<IProvider> GetProviders(string? filePath = null, string? language = null) =>
        PluginLoader
            .GetAssemblies()
            .Where(x =>
                string.IsNullOrWhiteSpace(filePath)
                || string.Equals(x.Location, filePath, StringComparison.OrdinalIgnoreCase)
            )
            .SelectMany(a => a.GetTypes())
            .Where(x =>
                x.GetInterfaces().Contains(typeof(IProvider))
                && x.GetConstructor(Type.EmptyTypes) is not null
            )
            .Select(x => (IProvider)Activator.CreateInstance(x, [_httpClientFactory])!)
            .Where(x =>
                string.IsNullOrEmpty(language)
                || x.Language.Equals(language, StringComparison.OrdinalIgnoreCase)
            )
            .ToList();

    public IList<Type> GetProviderTypes(string? filePath = null) =>
        PluginLoader
            .GetAssemblies()
            .Where(x =>
                string.IsNullOrWhiteSpace(filePath)
                || string.Equals(x.Location, filePath, StringComparison.OrdinalIgnoreCase)
            )
            .SelectMany(a => a.GetTypes())
            .Where(x =>
                x.GetInterfaces().Contains(typeof(IProvider))
                && x.GetConstructor(Type.EmptyTypes) is not null
            )
            .ToList();
}
