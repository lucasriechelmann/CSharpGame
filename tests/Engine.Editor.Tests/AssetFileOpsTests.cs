using Engine.Editor;

namespace Engine.Editor.Tests;

public sealed class AssetFileOpsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "assetops-" + Guid.NewGuid().ToString("N"));

    public AssetFileOpsTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "prefabs"));
        File.WriteAllText(Path.Combine(_root, "prefabs", "coin.prefab.json"), "{}");
        File.WriteAllText(Path.Combine(_root, "prefabs", "gem.prefab.json"), "{}");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Rename_moves_the_file_within_its_folder_and_returns_the_new_asset_name()
    {
        string renamed = AssetFileOps.RenameFile(_root, "prefabs/coin.prefab.json", "gold.prefab.json");

        Assert.Equal("prefabs/gold.prefab.json", renamed);
        Assert.True(File.Exists(Path.Combine(_root, "prefabs", "gold.prefab.json")));
        Assert.False(File.Exists(Path.Combine(_root, "prefabs", "coin.prefab.json")));
    }

    [Fact]
    public void Rename_never_overwrites_and_rejects_folders_in_the_name()
    {
        Assert.Throws<IOException>(() => AssetFileOps.RenameFile(_root, "prefabs/coin.prefab.json", "gem.prefab.json"));
        Assert.Throws<ArgumentException>(() => AssetFileOps.RenameFile(_root, "prefabs/coin.prefab.json", "../escape.json"));
        Assert.Throws<ArgumentException>(() => AssetFileOps.RenameFile(_root, "prefabs/coin.prefab.json", " "));
        Assert.True(File.Exists(Path.Combine(_root, "prefabs", "coin.prefab.json")));
    }

    [Fact]
    public void Paths_outside_the_asset_root_are_refused()
    {
        string outside = Path.Combine(Path.GetTempPath(), "assetops-outside-" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(outside, "keep me");
        try
        {
            string relative = Path.GetRelativePath(_root, outside);

            Assert.Throws<ArgumentException>(() => AssetFileOps.DeleteFile(_root, relative));
            Assert.Throws<ArgumentException>(() => AssetFileOps.RenameFile(_root, relative, "x.txt"));
            Assert.True(File.Exists(outside));
        }
        finally
        {
            File.Delete(outside);
        }
    }

    [Fact]
    public void Delete_removes_the_file_and_a_missing_one_throws()
    {
        AssetFileOps.DeleteFile(_root, "prefabs/gem.prefab.json");

        Assert.False(File.Exists(Path.Combine(_root, "prefabs", "gem.prefab.json")));
        Assert.Throws<FileNotFoundException>(() => AssetFileOps.DeleteFile(_root, "prefabs/gem.prefab.json"));
    }

    [Fact]
    public void CreateFolder_makes_a_folder_in_the_root_or_below_and_refuses_duplicates()
    {
        AssetFileOps.CreateFolder(_root, "", "sounds");
        AssetFileOps.CreateFolder(_root, "prefabs", "enemies");

        Assert.True(Directory.Exists(Path.Combine(_root, "sounds")));
        Assert.True(Directory.Exists(Path.Combine(_root, "prefabs", "enemies")));
        Assert.Throws<IOException>(() => AssetFileOps.CreateFolder(_root, "", "sounds"));
        Assert.Throws<ArgumentException>(() => AssetFileOps.CreateFolder(_root, "", "a/b"));
    }

    [Theory]
    [InlineData("hero.png", true)]
    [InlineData("HERO.PNG", true)]
    [InlineData("level.scene.json", false)]
    [InlineData("noextension", false)]
    public void Images_are_recognised_by_extension(string file, bool expected) => Assert.Equal(expected, AssetFileOps.IsImage(file));
}
