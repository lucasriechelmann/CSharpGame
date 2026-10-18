using System.Text.Json;
using Engine.Editor.Projects;

namespace Engine.Editor.Tests;

public sealed class ProjectKindTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "kinds-" + Guid.NewGuid().ToString("N"));

    public ProjectKindTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string Make(string name, ProjectKind kind)
    {
        string file = ProjectTemplate.Create(_folder, name, @"C:\Engine\", kind);
        return Path.GetDirectoryName(file)!;
    }

    [Theory]
    [InlineData("empty", ProjectKind.Empty2D)]
    [InlineData("platformer", ProjectKind.Platformer2D)]
    [InlineData("3D", ProjectKind.Scene3D)]
    public void Command_line_words_name_the_kinds(string word, ProjectKind expected)
    {
        Assert.True(ProjectKindInfo.TryParse(word, out var kind));
        Assert.Equal(expected, kind);
    }

    [Fact]
    public void An_unknown_word_is_not_a_kind_and_every_kind_has_text()
    {
        Assert.False(ProjectKindInfo.TryParse("roguelike", out _));
        foreach (var kind in Enum.GetValues<ProjectKind>())
        {
            Assert.NotEmpty(ProjectKindInfo.Title(kind));
            Assert.NotEmpty(ProjectKindInfo.Description(kind));
            Assert.True(ProjectKindInfo.TryParse(ProjectKindInfo.Keyword(kind), out var back));
            Assert.Equal(kind, back);
        }
    }

    [Fact]
    public void The_platformer_starter_has_a_tile_level_a_tile_set_an_atlas_and_a_hero()
    {
        string root = Make("Plat", ProjectKind.Platformer2D);

        Assert.True(File.Exists(Path.Combine(root, "Assets", "tiles.png")));
        Assert.True(File.Exists(Path.Combine(root, "Assets", "tilesets", "world.tileset.json")));
        string scene = File.ReadAllText(Path.Combine(root, "Assets", "scenes", "main.scene.json"));
        Assert.Contains("PlatformerIntent", scene);
        Assert.Contains("TileLayerRenderer", scene);
        string game = File.ReadAllText(Path.Combine(root, "Game", "PlatGame.cs"));
        Assert.Contains("class PlatGame : Game", game);
        Assert.DoesNotContain("__NAME__", game);
    }

    [Fact]
    public void The_platformer_level_is_a_full_grid_of_known_tiles_with_a_solid_floor()
    {
        string root = Make("Plat", ProjectKind.Platformer2D);

        using var level = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Assets", "levels", "ground.layer.json")));
        var rows = level.RootElement.GetProperty("rows").EnumerateArray().Select(row => row.EnumerateArray().Select(cell => cell.GetInt32()).ToArray()).ToArray();

        Assert.Equal(12, rows.Length);
        Assert.All(rows, row => Assert.Equal(30, row.Length));
        Assert.All(rows.SelectMany(row => row), tile => Assert.InRange(tile, 0, 2)); // the atlas has two tiles
        Assert.All(rows[^1], tile => Assert.NotEqual(0, tile)); // no holes in the floor
    }

    [Fact]
    public void The_3d_starter_has_materials_a_lit_scene_and_no_2d_sprite()
    {
        string root = Make("Box", ProjectKind.Scene3D);

        Assert.True(File.Exists(Path.Combine(root, "Assets", "materials", "floor.material.json")));
        Assert.False(File.Exists(Path.Combine(root, "Assets", "sprites", "square.png")));
        string scene = File.ReadAllText(Path.Combine(root, "Assets", "scenes", "main.scene.json"));
        Assert.Contains("DirectionalLight", scene);
        Assert.Contains("MeshRenderer", scene);
        Assert.Contains("FlyCamera", File.ReadAllText(Path.Combine(root, "Game", "BoxGame.cs")));
    }

    [Fact]
    public void Every_kind_still_has_the_shared_project_files()
    {
        foreach (var kind in Enum.GetValues<ProjectKind>())
        {
            string root = Make("P" + kind, kind);

            Assert.True(File.Exists(Path.Combine(root, "Directory.Build.props")));
            Assert.True(File.Exists(Path.Combine(root, "Launcher", "Program.cs")));
            Assert.True(File.Exists(Path.Combine(root, "Game", "P" + kind + "Module.cs")));
            Assert.DoesNotContain("__NAME__", File.ReadAllText(Path.Combine(root, "Assets", "scenes", "main.scene.json")));
        }
    }
}
