using System;
using Juro.Core;

namespace Juro.Providers;

public class Config : IClientConfig
{
    public string RepositoryUrl => "https://github.com/jerry08/Juro";

    public Version MinimumSupportedVersion => new(1, 0, 0);
}
