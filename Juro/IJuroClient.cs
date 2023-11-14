using System.Collections.Generic;
using Juro.Core.Providers;

namespace Juro;

public interface IJuroClient<IProvider>
    where IProvider : ISourceProvider
{
    IList<IProvider> GetAllProviders();

    IList<IProvider> GetProviders(string? language = null);
}

public interface IJuroClient : IJuroClient<ISourceProvider> { }
