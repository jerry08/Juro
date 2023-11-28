using System;
using Juro.Core.Models;

namespace Juro.Core.Attributes;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class PluginAssemblyAttribute : Attribute
{
    public AssemblyPluginType PluginType { get; } = AssemblyPluginType.None;

    public PluginAssemblyAttribute() { }

    public PluginAssemblyAttribute(AssemblyPluginType type)
    {
        PluginType = type;
    }
}
