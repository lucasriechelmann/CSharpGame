using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;

namespace Engine.Scenes.Tests.Fixtures;

/// <summary>
/// A world, asset manager (rooted in a unique temp folder), component registry and scene serializer wired together, plus
/// helpers to write files into the asset root. Deletes its folder when disposed.
/// </summary>
internal sealed class SceneTestBed : IDisposable
{
    public SceneTestBed()
    {
        Root = Path.Combine(Path.GetTempPath(), "csharpgame-scenes-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);

        World = new World();
        Assets = new AssetManager(Root);
        Assets.Register(new PrefabLoader());
        Assets.Register(new TextLoader());

        Registry = new ComponentRegistry()
            .Register<Name>("Name")
            .Register<Transform>("Transform")
            .Register<Health>("Health")
            .Register<Stance>("Stance")
            .Register<TextRef>("TextRef");

        Scenes = new SceneSerializer(World, Assets, Registry);
    }

    public string Root { get; }

    public World World { get; }

    public AssetManager Assets { get; }

    public ComponentRegistry Registry { get; }

    public SceneSerializer Scenes { get; }

    /// <summary>Writes a file under the asset root and returns its relative path.</summary>
    public string Write(string relativePath, string content)
    {
        string full = Path.Combine(Root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return relativePath;
    }

    public void Dispose()
    {
        Assets.Dispose();
        Directory.Delete(Root, recursive: true);
    }
}
