using System;
using Juro.Core.Models;

namespace Juro.Core.Attributes;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class ModuleAssemblyAttribute : Attribute
{
    public AssemblyPluginType PluginType { get; } = AssemblyPluginType.None;

    public ModuleAssemblyAttribute() { }

    public ModuleAssemblyAttribute(AssemblyPluginType type)
    {
        PluginType = type;
    }
}
