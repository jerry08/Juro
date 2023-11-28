using System.Collections.Generic;
using Juro.Core.Providers;

namespace Juro;

public interface IJuroClient<IProvider>
    where IProvider : ISourceProvider
{
    /// <summary>
    /// Gets all providers in currenly loaded plugin.
    /// </summary>
    IList<IProvider> GetAllProviders();

    /// <summary>
    /// Gets providers in currenly loaded assemblies.
    /// </summary>
    /// <param name="filePath">File path to plugin.</param>
    /// <param name="language">Language (Culture name) of the provider.</param>
    IList<IProvider> GetProviders(string? filePath = null, string? language = null);
}

public interface IJuroClient : IJuroClient<ISourceProvider> { }
