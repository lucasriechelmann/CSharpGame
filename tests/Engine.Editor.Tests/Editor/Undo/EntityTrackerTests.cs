using Engine.Core.Ecs;
using Engine.Runtime.Editing;

namespace Engine.Editor.Tests.Editor.Undo;

public class EntityTrackerTests
{
    private readonly World _world = new();
    private readonly EntityTracker _tracker = new();

    [Fact]
    public void An_entity_keeps_the_same_id()
    {
        var entity = _world.CreateEntity();

        Assert.Equal(_tracker.IdOf(entity), _tracker.IdOf(entity));
    }

    [Fact]
    public void Different_entities_get_different_ids()
    {
        Assert.NotEqual(_tracker.IdOf(_world.CreateEntity()), _tracker.IdOf(_world.CreateEntity()));
    }

    [Fact]
    public void Lookup_returns_the_tracked_entity()
    {
        var entity = _world.CreateEntity();

        Assert.Equal(entity, _tracker.Lookup(_tracker.IdOf(entity)));
    }

    [Fact]
    public void An_unknown_id_looks_up_as_null()
    {
        Assert.True(_tracker.Lookup(99).IsNull);
    }

    [Fact]
    public void Unbinding_clears_the_entity_but_keeps_the_id_for_reuse()
    {
        var entity = _world.CreateEntity();
        int id = _tracker.IdOf(entity);

        _tracker.Unbind(id);
        Assert.True(_tracker.Lookup(id).IsNull);

        var recreated = _world.CreateEntity();
        _tracker.Bind(id, recreated);
        Assert.Equal(recreated, _tracker.Lookup(id));
        Assert.Equal(id, _tracker.IdOf(recreated));
    }

    [Fact]
    public void Rebinding_an_id_releases_the_old_entity()
    {
        var first = _world.CreateEntity();
        int id = _tracker.IdOf(first);
        var second = _world.CreateEntity();

        _tracker.Bind(id, second);

        Assert.NotEqual(id, _tracker.IdOf(first)); // the old handle is a stranger again and gets a fresh id
    }

    [Fact]
    public void Clear_forgets_everything()
    {
        int id = _tracker.IdOf(_world.CreateEntity());

        _tracker.Clear();

        Assert.True(_tracker.Lookup(id).IsNull);
    }

    [Fact]
    public void The_null_entity_cannot_be_tracked()
    {
        Assert.Throws<ArgumentException>(() => _tracker.IdOf(Entity.Null));
    }
}
