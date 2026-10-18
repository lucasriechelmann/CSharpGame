using Engine.Core.Components;
using Engine.Scenes.Tests.Fixtures;

namespace Engine.Scenes.Tests;

public sealed class PrefabEditingTests : IDisposable
{
    private readonly SceneTestBed _bed = new();

    public void Dispose() => _bed.Dispose();

    [Fact]
    public void SaveAsPrefab_writes_a_file_and_links_the_entity()
    {
        var e = _bed.World.CreateEntity();
        _bed.World.Add(e, Name.Of("Gem"));
        _bed.World.Add(e, new Health { Value = 3 });

        _bed.Scenes.SaveAsPrefab(e, "prefabs/gem.prefab.json");

        Assert.Equal("prefabs/gem.prefab.json", _bed.World.Get<PrefabLink>(e).Prefab);
        var copy = _bed.Scenes.Instantiate("prefabs/gem.prefab.json");
        Assert.Equal(3, _bed.World.Get<Health>(copy).Value);
    }

    [Fact]
    public void SaveAsPrefab_with_an_empty_name_throws()
    {
        var e = _bed.World.CreateEntity();

        Assert.Throws<ArgumentException>(() => _bed.Scenes.SaveAsPrefab(e, " "));
    }

    [Fact]
    public void RevertToPrefab_restores_changed_values_and_removes_added_components()
    {
        _bed.Write("prefabs/gem.prefab.json", """{ "components": { "Name": { "value": "Gem" }, "Health": { "value": 3 } } }""");
        var e = _bed.Scenes.Instantiate("prefabs/gem.prefab.json");
        _bed.World.Set(e, new Health { Value = 99 });
        _bed.World.Remove<Name>(e);
        _bed.World.Add(e, new Stance { Mode = StanceMode.Crouching });

        _bed.Scenes.RevertToPrefab(e);

        Assert.Equal(3, _bed.World.Get<Health>(e).Value);
        Assert.Equal("Gem", _bed.World.Get<Name>(e).Value);
        Assert.False(_bed.World.Has<Stance>(e));
        Assert.True(_bed.World.Has<PrefabLink>(e));
    }

    [Fact]
    public void RevertToPrefab_on_a_plain_entity_throws()
    {
        var e = _bed.World.CreateEntity();

        Assert.Throws<InvalidOperationException>(() => _bed.Scenes.RevertToPrefab(e));
    }
}
