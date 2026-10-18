using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics2D;
using Engine.Editor;

namespace Engine.Editor.Tests.Editor;

public class EntityClonerTests
{
    private readonly World _world = new();

    private Entity AddOriginal()
    {
        var entity = _world.CreateEntity();
        _world.Add(entity, Transform.At(10f, 20f, 3f));
        _world.Add(entity, Name.Of("Crate"));
        _world.Add(entity, BoxCollider.Create(new Vector2(16f, 16f)));
        return entity;
    }

    [Fact]
    public void The_copy_has_every_component_of_the_original()
    {
        var original = AddOriginal();

        var copy = EntityCloner.Duplicate(_world, original, Vector2.Zero);

        Assert.NotEqual(original, copy);
        Assert.True(_world.Has<Transform>(copy));
        Assert.True(_world.Has<Name>(copy));
        Assert.Equal(new Vector2(16f, 16f), _world.Get<BoxCollider>(copy).Size);
    }

    [Fact]
    public void The_copy_is_offset_and_the_original_stays_put()
    {
        var original = AddOriginal();

        var copy = EntityCloner.Duplicate(_world, original, new Vector2(32f, -16f));

        Assert.Equal(new Vector3(42f, 4f, 3f), _world.Get<Transform>(copy).Position);
        Assert.Equal(new Vector3(10f, 20f, 3f), _world.Get<Transform>(original).Position);
    }

    [Fact]
    public void The_copy_name_says_copy()
    {
        var original = AddOriginal();

        var copy = EntityCloner.Duplicate(_world, original, Vector2.Zero);

        Assert.Equal("Crate (copy)", _world.Get<Name>(copy).Value);
        Assert.Equal("Crate", _world.Get<Name>(original).Value);
    }

    [Fact]
    public void Editing_the_copy_does_not_touch_the_original()
    {
        var original = AddOriginal();
        var copy = EntityCloner.Duplicate(_world, original, Vector2.Zero);

        _world.Get<BoxCollider>(copy).Size = new Vector2(99f, 99f);

        Assert.Equal(new Vector2(16f, 16f), _world.Get<BoxCollider>(original).Size);
    }

    [Fact]
    public void The_copy_has_no_render_history()
    {
        var original = AddOriginal();
        _world.Add(original, PreviousTransform.From(Transform.At(1f, 1f)));

        var copy = EntityCloner.Duplicate(_world, original, Vector2.Zero);

        Assert.False(_world.Get<PreviousTransform>(copy).HasHistory);
        Assert.True(_world.Get<PreviousTransform>(original).HasHistory);
    }

    [Fact]
    public void An_entity_with_no_components_can_be_copied()
    {
        var original = _world.CreateEntity();

        var copy = EntityCloner.Duplicate(_world, original, new Vector2(5f, 5f));

        Assert.True(_world.IsAlive(copy));
    }

    [Fact]
    public void Copying_a_dead_entity_fails()
    {
        var original = AddOriginal();
        _world.DestroyEntity(original);

        Assert.Throws<InvalidOperationException>(() => EntityCloner.Duplicate(_world, original, Vector2.Zero));
    }
}
