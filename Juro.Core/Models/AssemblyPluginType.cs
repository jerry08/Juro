using System;

namespace Juro.Core.Models;

[Flags]
public enum AssemblyPluginType
{
    None = 0,
    Movie = 1,
    Manga = 2,
    Anime = 3,
    Novel = 4
}
