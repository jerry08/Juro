﻿using System;
using System.Diagnostics;
using System.IO;

namespace Juro;

public class Module
{
    public string Name { get; set; } = default!;

    public string FilePath { get; set; } = default!;

    public Version? Version { get; set; }

    public Module() { }

    public Module(string? name, string filePath)
    {
        Name = name ?? Path.GetFileNameWithoutExtension(filePath);
        FilePath = filePath;

        _ = Version.TryParse(FileVersionInfo.GetVersionInfo(filePath).FileVersion, out var version);
        Version = version;
    }

    public override string ToString() => $"{Name} {Version}";
}