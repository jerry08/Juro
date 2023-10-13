using System;

namespace Juro.Core;

public interface IClientConfig
{
    string RepositoryUrl { get; }

    Version MinimumSupportedVersion { get; }
}
