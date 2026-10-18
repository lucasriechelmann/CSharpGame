using Engine.Editor;

namespace Engine.Editor.Tests;

public sealed class AssetFolderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "assets-" + Guid.NewGuid().ToString("N"));

    public AssetFolderTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "prefabs"));
        Directory.CreateDirectory(Path.Combine(_root, "levels"));
        File.WriteAllText(Path.Combine(_root, "hero.png"), "x");
        File.WriteAllText(Path.Combine(_root, "prefabs", "coin.prefab.json"), "{}");
        File.WriteAllText(Path.Combine(_root, "prefabs", "Bat.prefab.json"), "{}");
        File.WriteAllText(Path.Combine(_root, "prefabs", "readme.txt"), "x");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void The_tree_lists_folders_and_files_sorted_ignoring_case()
    {
        var root = AssetFolder.Scan(_root);

        Assert.Equal(new[] { "levels", "prefabs" }, root.Folders.Select(f => f.Name));
        Assert.Equal(new[] { "hero.png" }, root.Files);
        Assert.Equal(new[] { "Bat.prefab.json", "coin.prefab.json", "readme.txt" }, root.Folders[1].Files);
    }

    [Fact]
    public void Asset_names_are_relative_with_forward_slashes_like_games_load_them()
    {
        var root = AssetFolder.Scan(_root);

        Assert.Equal("hero.png", root.AssetName("hero.png"));
        Assert.Equal("prefabs/coin.prefab.json", root.Folders[1].AssetName("coin.prefab.json"));
    }

    [Fact]
    public void Only_prefab_files_count_as_prefabs()
    {
        Assert.True(AssetFolder.IsPrefab("coin.prefab.json"));
        Assert.True(AssetFolder.IsPrefab("COIN.PREFAB.JSON"));
        Assert.False(AssetFolder.IsPrefab("level.scene.json"));
        Assert.False(AssetFolder.IsPrefab("hero.png"));
    }

    [Fact]
    public void A_missing_root_is_an_empty_tree()
    {
        var root = AssetFolder.Scan(Path.Combine(_root, "nope"));

        Assert.Empty(root.Folders);
        Assert.Empty(root.Files);
    }
}
