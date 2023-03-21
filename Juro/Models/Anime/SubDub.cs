namespace Juro.Models.Anime;

/// <summary>
/// Used to select sub or dub for anime.
/// </summary>
public enum SubDub
{
    /// <summary>
    /// Selects all available (sub and dub).
    /// </summary>
    All,

    /// <summary>
    /// Selects sub.
    /// </summary>
    Sub,

    /// <summary>
    /// Selects dub.
    /// </summary>
    Dub
}