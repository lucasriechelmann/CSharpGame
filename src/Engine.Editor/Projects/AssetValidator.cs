using Engine.Animation;
using Engine.Audio;
using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Ecs;
using Engine.Rendering;
using Engine.Rendering3D;
using Engine.Scenes;
using Engine.Tilemaps;

namespace Engine.Editor.Projects;

/// <summary>
/// Loads every asset file of a project the way the game would, so a broken scene, a missing texture or a bad material is found now and not when
/// a player reaches it. It uses the running game's loaders (so it needs the GL context the editor has) and loads scenes into a scratch world, leaving the open scene alone.
/// </summary>
public static class AssetValidator
{
    /// <summary>Tries to load every recognised file under the asset root.</summary>
    /// <param name="root">The asset root folder.</param>
    /// <param name="assets">The game's asset manager (its loaders do the work; loaded assets stay cached, as in play).</param>
    /// <param name="components">The game's component registry, needed to read scenes.</param>
    /// <param name="checkedCount">How many files were tried.</param>
    /// <returns>The files that failed, empty when everything loads.</returns>
    public static IReadOnlyList<AssetProblem> Validate(string root, AssetManager assets, ComponentRegistry components, out int checkedCount)
    {
        var problems = new List<AssetProblem>();
        int tried = 0;
        if (!Directory.Exists(root))
        {
            checkedCount = 0;
            return problems;
        }

        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            string name = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
            Action<string>? check = CheckFor(name, assets, components);
            if (check is null) continue;

            tried++;
            try
            {
                check(name);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                problems.Add(new AssetProblem(name, ex.InnerException?.Message ?? ex.Message));
            }
        }

        checkedCount = tried;
        return problems;
    }

    /// <summary>Which kind of asset a file is, by its name, as a loading action; null for files that are not assets the engine loads by themselves.</summary>
    private static Action<string>? CheckFor(string name, AssetManager assets, ComponentRegistry components)
    {
        string lower = name.ToLowerInvariant();
        if (lower.EndsWith(".scene.json", StringComparison.Ordinal))
            return n => new SceneSerializer(new World(), assets, components).LoadFile(n);
        if (lower.EndsWith(".prefab.json", StringComparison.Ordinal)) return n => assets.Load<Prefab>(n);
        if (lower.EndsWith(".layer.json", StringComparison.Ordinal)) return n => assets.Load<TileLayer>(n);
        if (lower.EndsWith(".tileset.json", StringComparison.Ordinal)) return n => assets.Load<TileSet>(n);
        if (lower.EndsWith(".anim.json", StringComparison.Ordinal)) return n => assets.Load<AnimationStateMachine>(n);
        if (lower.EndsWith(".material.json", StringComparison.Ordinal)) return n => assets.Load<Material>(n);
        if (lower.EndsWith(".png", StringComparison.Ordinal)) return n => assets.Load<Texture2D>(n);
        if (lower.EndsWith(".wav", StringComparison.Ordinal)) return n => assets.Load<AudioClip>(n);
        if (lower.EndsWith(".gltf", StringComparison.Ordinal) || lower.EndsWith(".glb", StringComparison.Ordinal)) return n => assets.Load<Model>(n);
        return null;
    }
}
