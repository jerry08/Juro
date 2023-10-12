using System.Collections.Generic;
using System.Reflection;

namespace Juro.Utils;

internal static class AssemblyEx
{
    public static IEnumerable<Assembly> GetReferencedAssemblies()
    {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly is null)
            yield break;

        foreach (var reference in assembly.GetReferencedAssemblies())
        {
            yield return Assembly.Load(reference);
        }
    }

    public static IEnumerable<Assembly> GetAllAssemblies()
    {
        var asm = Assembly.GetEntryAssembly();
        //var asm = Assembly.GetExecutingAssembly();
        if (asm is null)
            yield break;

        var list = new List<string>();
        var stack = new Stack<Assembly>();
        stack.Push(asm);

        do
        {
            var assembly = stack.Pop();

            yield return assembly;

            var gg = assembly.GetReferencedAssemblies();

            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (!list.Contains(reference.FullName))
                {
                    stack.Push(Assembly.Load(reference));
                    list.Add(reference.FullName);
                }
            }
        } while (stack.Count > 0);
    }
}
