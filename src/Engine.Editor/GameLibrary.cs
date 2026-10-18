using System.Reflection;
using System.Runtime.Loader;
using Engine.Runtime;

namespace Engine.Editor;

/// <summary>
/// Loads a game library (a DLL such as <c>Platformer.Game.dll</c>) and finds its <see cref="IGameModule"/>. The engine assemblies the
/// game uses are the editor's own, already loaded, so game and editor share one copy of every engine type; only the game's own
/// assemblies (and anything else beside its DLL that its <c>.deps.json</c> lists) are resolved from the game's folder.
/// </summary>
public static class GameLibrary
{
    /// <summary>Loads the library and creates its module.</summary>
    /// <param name="dllPath">Path to the game library DLL.</param>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidOperationException">The library has no module, or more than one.</exception>
    public static IGameModule Load(string dllPath)
    {
        string full = Path.GetFullPath(dllPath);
        if (!File.Exists(full)) throw new FileNotFoundException($"Game library '{dllPath}' not found.", full);

        var resolver = new AssemblyDependencyResolver(full);
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            string? path = resolver.ResolveAssemblyToPath(name);
            return path == null ? null : context.LoadFromAssemblyPath(path);
        };

        return CreateModule(Assembly.LoadFrom(full));
    }

    /// <summary>Creates the one <see cref="IGameModule"/> an assembly contains.</summary>
    /// <param name="assembly">The game's assembly.</param>
    /// <exception cref="InvalidOperationException">There is no module, more than one, or it has no public parameterless constructor.</exception>
    public static IGameModule CreateModule(Assembly assembly)
    {
        var candidates = assembly.GetExportedTypes()
            .Where(t => typeof(IGameModule).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            .ToList();

        if (candidates.Count == 0)
            throw new InvalidOperationException($"{assembly.GetName().Name} contains no public class implementing IGameModule.");
        if (candidates.Count > 1)
            throw new InvalidOperationException($"{assembly.GetName().Name} contains several game modules ({string.Join(", ", candidates.Select(t => t.Name))}); a game library must have exactly one.");

        return Activator.CreateInstance(candidates[0]) as IGameModule
            ?? throw new InvalidOperationException($"{candidates[0].Name} could not be created (it needs a public parameterless constructor).");
    }
}
