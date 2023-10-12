namespace Juro.Core.Providers;

/// <summary>
/// Base interface for main providers.
/// </summary>
public interface ISourceProvider
{
    /// <summary>
    /// Name of the provider.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Language of the provider.
    /// </summary>
    public string Language { get; }
}
