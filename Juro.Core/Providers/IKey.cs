namespace Juro.Core.Providers;

/// <summary>
/// Interface to get unique unchanging key from providers.
/// </summary>
public interface IKey
{
    /// <summary>
    /// Unique unchanging key of the provider.
    /// </summary>
    public string Key { get; }
}
