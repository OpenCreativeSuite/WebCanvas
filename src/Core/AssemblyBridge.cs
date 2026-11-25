using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace WebCanvasDotNet.Core;

/// <summary>
/// The component package on NuGet is compiled against a later Donkeh.UILab
/// assembly version than the local framework checkout this application builds
/// against. The public surface is compatible, so resolve the package's request
/// to the already-loaded local assembly instead of failing at first use.
/// </summary>
internal static class AssemblyBridge
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            if (!string.Equals(name.Name, "Donkeh.UILab", StringComparison.Ordinal))
            {
                return null;
            }

            return typeof(Donkeh.UILab.Component).Assembly;
        };
    }
}
