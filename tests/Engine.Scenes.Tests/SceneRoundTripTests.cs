using System.Numerics;
using System.Text.Json.Nodes;
using Engine.Core.Components;
using Engine.Scenes.Tests.Fixtures;

namespace Engine.Scenes.Tests;

public sealed class SceneRoundTripTests : IDisposable
{
    private readonly SceneTestBed _bed = new();

    public void Dispose() => _bed.Dispose();

    [Fact]
    public void An_empty_world_saves_a_versioned_scene_with_no_entities()
    {
        var root = JsonNode.Parse(_bed.Scenes.Save())!;

        Assert.Equal(1, root["version"]!.GetValue<int>());
        Assert.Empty(root["entities"]!.AsArray());
    }

    [Fact]
    public void Entities_and_components_survive_a_save_and_load()
    {
        var e = _bed.World.CreateEntity();
        var transform = Transform.At(new Vector3(1.5f, -2f, 3f));
        transform.Scale = new Vector3(2f, 3f, 1f);
        transform.SetRotationZ(0.5f);
        _bed.World.Add(e, transform);
        _bed.World.Add(e, Name.Of("Hero"));
        _bed.World.Add(e, new Health { Value = 42 });

        string json = _bed.Scenes.Save();

        using var other = new SceneTestBed();
        var loaded = Assert.Single(other.Scenes.Load(json));
        var t = other.World.Get<Transform>(loaded);
        Assert.Equal(new Vector3(1.5f, -2f, 3f), t.Position);
        Assert.Equal(new Vector3(2f, 3f, 1f), t.Scale);
        Assert.Equal(0.5f, t.RotationZ, 4);
        Assert.Equal("Hero", other.World.Get<Name>(loaded).Value);
        Assert.Equal(42, other.World.Get<Health>(loaded).Value);
    }

    [Fact]
    public void Entity_order_and_component_sets_are_preserved()
    {
        var a = _bed.World.CreateEntity();
        _bed.World.Add(a, Name.Of("first"));
        _bed.World.Add(a, new Health { Value = 1 });
        var b = _bed.World.CreateEntity();
        _bed.World.Add(b, Name.Of("second"));
        var c = _bed.World.CreateEntity(); // no components at all

        using var other = new SceneTestBed();
        var loaded = other.Scenes.Load(_bed.Scenes.Save());

        Assert.Equal(3, loaded.Count);
        Assert.Equal("first", other.World.Get<Name>(loaded[0]).Value);
        Assert.True(other.World.Has<Health>(loaded[0]));
        Assert.Equal("second", other.World.Get<Name>(loaded[1]).Value);
        Assert.False(other.World.Has<Health>(loaded[1]));
        Assert.False(other.World.Has<Name>(loaded[2]));
    }

    [Fact]
    public void Unregistered_components_are_not_saved()
    {
        var e = _bed.World.CreateEntity();
        _bed.World.Add(e, new Health { Value = 5 });
        _bed.World.Add(e, new UnregisteredComponent { Value = 9 });

        var json = JsonNode.Parse(_bed.Scenes.Save())!;

        var components = json["entities"]![0]!["components"]!.AsObject();
        Assert.True(components.ContainsKey("Health"));
        Assert.Single(components);
    }

    [Fact]
    public void Json_uses_camel_case_names_and_skips_computed_properties()
    {
        var e = _bed.World.CreateEntity();
        _bed.World.Add(e, Transform.At(1, 2, 3));

        var transform = JsonNode.Parse(_bed.Scenes.Save())!["entities"]![0]!["components"]!["Transform"]!;

        Assert.Equal(1f, transform["position"]!["x"]!.GetValue<float>());
        Assert.NotNull(transform["rotation"]!["w"]);
        Assert.Null(transform["rotationZ"]); // computed from the quaternion, not stored
    }

    [Fact]
    public void Enums_are_written_as_text_and_read_back()
    {
        var e = _bed.World.CreateEntity();
        _bed.World.Add(e, new Stance { Mode = StanceMode.Crouching });

        string json = _bed.Scenes.Save();
        Assert.Contains("\"Crouching\"", json);

        using var other = new SceneTestBed();
        var loaded = Assert.Single(other.Scenes.Load(json));
        Assert.Equal(StanceMode.Crouching, other.World.Get<Stance>(loaded).Mode);
    }

    [Fact]
    public void Asset_handles_are_saved_by_name_and_resolved_on_load()
    {
        _bed.Write("notes/hello.txt", "hello");
        var handle = _bed.Assets.Load<TextAsset>("notes/hello.txt");
        var e = _bed.World.CreateEntity();
        _bed.World.Add(e, new TextRef { Asset = handle });

        string json = _bed.Scenes.Save();
        Assert.Contains("\"notes/hello.txt\"", json);

        using var other = new SceneTestBed();
        other.Write("notes/hello.txt", "hello from the other side");
        var loaded = Assert.Single(other.Scenes.Load(json));
        var reference = other.World.Get<TextRef>(loaded);
        Assert.Equal("hello from the other side", other.Assets.Get(reference.Asset).Text);
    }

    [Fact]
    public void In_memory_assets_round_trip_by_name_when_they_still_exist()
    {
        var handle = _bed.Assets.Add("builtin/note", new TextAsset("made in code"));
        var e = _bed.World.CreateEntity();
        _bed.World.Add(e, new TextRef { Asset = handle });

        string json = _bed.Scenes.Save();
        _bed.World.Clear();
        var loaded = Assert.Single(_bed.Scenes.Load(json));

        Assert.Equal(handle, _bed.World.Get<TextRef>(loaded).Asset);
    }

    [Fact]
    public void An_invalid_handle_is_saved_as_null_and_loads_as_invalid()
    {
        var e = _bed.World.CreateEntity();
        _bed.World.Add(e, new TextRef());

        string json = _bed.Scenes.Save();
        Assert.Contains("null", json);

        using var other = new SceneTestBed();
        var loaded = Assert.Single(other.Scenes.Load(json));
        Assert.False(other.World.Get<TextRef>(loaded).Asset.IsValid);
    }

    [Fact]
    public void Saving_to_a_file_creates_folders_and_loading_resolves_relative_to_the_asset_root()
    {
        var e = _bed.World.CreateEntity();
        _bed.World.Add(e, new Health { Value = 7 });

        _bed.Scenes.SaveToFile("saves/nested/slot1.scene.json");
        Assert.True(File.Exists(Path.Combine(_bed.Root, "saves", "nested", "slot1.scene.json")));

        _bed.World.Clear();
        var loaded = Assert.Single(_bed.Scenes.LoadFile("saves/nested/slot1.scene.json"));
        Assert.Equal(7, _bed.World.Get<Health>(loaded).Value);
    }

    [Fact]
    public void Loading_a_missing_file_throws_FileNotFound()
    {
        Assert.Throws<FileNotFoundException>(() => _bed.Scenes.LoadFile("nope.scene.json"));
    }

    [Fact]
    public void Loading_adds_to_the_existing_world_instead_of_replacing_it()
    {
        _bed.World.CreateEntity();
        string json = _bed.Scenes.Save();

        _bed.Scenes.Load(json);

        Assert.Equal(2, _bed.World.EntityCount);
    }

    private struct UnregisteredComponent
    {
        public int Value;
    }
}
