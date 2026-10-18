using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics2D;
using Engine.Runtime.Editor.Undo;

namespace Engine.Runtime.Tests.Editor.Undo;

public class EditCommandTests
{
    private readonly World _world = new();
    private readonly CommandStack _stack;

    public EditCommandTests() => _stack = new CommandStack(new EditContext(_world));

    private EditContext Context => _stack.Context;

    private Entity AddCrate(float x = 10f, float y = 20f)
    {
        var entity = _world.CreateEntity();
        _world.Add(entity, Transform.At(x, y));
        _world.Add(entity, Name.Of("Crate"));
        return entity;
    }

    private object Boxed(Entity entity, Type type) => _world.GetBoxed(entity, type);

    // ---- SetComponentCommand ----

    [Fact]
    public void A_recorded_move_undoes_and_redoes()
    {
        var crate = AddCrate();
        object before = Boxed(crate, typeof(Transform));
        _world.Get<Transform>(crate).Position.X = 99f; // the live drag
        object after = Boxed(crate, typeof(Transform));
        _stack.Push(new SetComponentCommand(Context, crate, typeof(Transform), before, after, "Crate"));

        _stack.Undo();
        Assert.Equal(10f, _world.Get<Transform>(crate).Position.X);

        _stack.Redo();
        Assert.Equal(99f, _world.Get<Transform>(crate).Position.X);
    }

    [Fact]
    public void A_move_is_described_by_the_entity()
    {
        var crate = AddCrate();
        var command = new SetComponentCommand(Context, crate, typeof(Transform), Boxed(crate, typeof(Transform)), Boxed(crate, typeof(Transform)), "Crate #3");

        Assert.Equal("Move Crate #3", command.Description);
    }

    [Fact]
    public void Another_component_edit_names_the_component()
    {
        var crate = AddCrate();
        _world.Add(crate, BoxCollider.Create(new Vector2(8f, 8f)));
        var value = Boxed(crate, typeof(BoxCollider));

        Assert.Equal("Edit BoxCollider of Crate", new SetComponentCommand(Context, crate, typeof(BoxCollider), value, value, "Crate").Description);
    }

    [Fact]
    public void Writing_a_transform_clears_the_render_history()
    {
        var crate = AddCrate();
        _world.Add(crate, PreviousTransform.From(Transform.At(1f, 1f)));
        object before = Boxed(crate, typeof(Transform));
        _world.Get<Transform>(crate).Position.X = 50f;
        _stack.Push(new SetComponentCommand(Context, crate, typeof(Transform), before, Boxed(crate, typeof(Transform)), "Crate"));

        _world.Get<PreviousTransform>(crate) = PreviousTransform.From(Transform.At(7f, 7f));
        _stack.Undo();

        Assert.False(_world.Get<PreviousTransform>(crate).HasHistory);
    }

    [Fact]
    public void Setting_a_component_of_a_missing_entity_fails_loudly()
    {
        var crate = AddCrate();
        var command = new SetComponentCommand(Context, crate, typeof(Transform), Boxed(crate, typeof(Transform)), Boxed(crate, typeof(Transform)), "Crate");
        _world.DestroyEntity(crate);

        Assert.Throws<InvalidOperationException>(() => command.Do(Context));
    }

    // ---- Add / Remove ----

    [Fact]
    public void Adding_a_component_undoes_by_removing_it()
    {
        var crate = AddCrate();

        _stack.Execute(new AddComponentCommand(Context, crate, typeof(BoxCollider), BoxCollider.Create(new Vector2(4f, 4f)), "Crate"));
        Assert.Equal(new Vector2(4f, 4f), _world.Get<BoxCollider>(crate).Size);

        _stack.Undo();
        Assert.False(_world.Has<BoxCollider>(crate));

        _stack.Redo();
        Assert.True(_world.Has<BoxCollider>(crate));
    }

    [Fact]
    public void Adding_with_no_value_adds_the_default()
    {
        var crate = AddCrate();

        _stack.Execute(new AddComponentCommand(Context, crate, typeof(PhysicsBody), null, "Crate"));

        Assert.True(_world.Has<PhysicsBody>(crate));
    }

    [Fact]
    public void Removing_a_component_undoes_by_restoring_its_value()
    {
        var crate = AddCrate();
        _world.Add(crate, BoxCollider.Create(new Vector2(12f, 6f)));

        _stack.Execute(new RemoveComponentCommand(Context, crate, typeof(BoxCollider), "Crate"));
        Assert.False(_world.Has<BoxCollider>(crate));

        _stack.Undo();
        Assert.Equal(new Vector2(12f, 6f), _world.Get<BoxCollider>(crate).Size);
    }

    [Fact]
    public void Redoing_a_removal_removes_the_current_value()
    {
        var crate = AddCrate();
        _world.Add(crate, BoxCollider.Create(new Vector2(12f, 6f)));
        _stack.Execute(new RemoveComponentCommand(Context, crate, typeof(BoxCollider), "Crate"));
        _stack.Undo();

        _stack.Redo();

        Assert.False(_world.Has<BoxCollider>(crate));
        _stack.Undo();
        Assert.Equal(new Vector2(12f, 6f), _world.Get<BoxCollider>(crate).Size);
    }

    // ---- Create / Delete ----

    [Fact]
    public void Deleting_an_entity_undoes_by_recreating_it_with_its_components()
    {
        var crate = AddCrate(x: 33f);

        _stack.Execute(EntityLifetimeCommand.ForDeleted(Context, crate, "Crate"));
        Assert.False(_world.IsAlive(crate));

        _stack.Undo();

        var restored = _world.Entities.Single();
        Assert.True(_world.IsAlive(restored));
        Assert.Equal(33f, _world.Get<Transform>(restored).Position.X);
        Assert.Equal("Crate", _world.Get<Name>(restored).Value);
    }

    [Fact]
    public void Undoing_a_creation_destroys_the_entity_and_redo_brings_it_back()
    {
        var crate = AddCrate();
        _stack.Push(EntityLifetimeCommand.ForCreated(Context, crate, "Crate"));

        _stack.Undo();
        Assert.False(_world.IsAlive(crate));
        Assert.Equal(0, _world.EntityCount);

        _stack.Redo();
        Assert.Equal(1, _world.EntityCount);
    }

    [Fact]
    public void Affected_is_null_when_the_entity_does_not_exist_any_more()
    {
        var crate = AddCrate();
        var command = EntityLifetimeCommand.ForCreated(Context, crate, "Crate");
        _stack.Push(command);

        Assert.False(command.Affected(Context).IsNull);
        _stack.Undo();
        Assert.True(command.Affected(Context).IsNull);
    }

    [Fact]
    public void Earlier_edits_still_find_an_entity_that_was_deleted_and_restored()
    {
        // Move, delete, then undo the delete and the move: the move command was made against a handle that no longer exists.
        var crate = AddCrate();
        object before = Boxed(crate, typeof(Transform));
        _world.Get<Transform>(crate).Position.X = 80f;
        _stack.Push(new SetComponentCommand(Context, crate, typeof(Transform), before, Boxed(crate, typeof(Transform)), "Crate"));
        _stack.Execute(EntityLifetimeCommand.ForDeleted(Context, crate, "Crate"));

        _stack.Undo(); // restore
        _stack.Undo(); // undo the move on the restored entity

        Assert.Equal(1, _world.EntityCount);
        var restored = _world.Entities.Single();
        Assert.Equal(10f, _world.Get<Transform>(restored).Position.X);
    }

    [Fact]
    public void A_restored_entity_comes_back_as_it_was_just_before_deletion()
    {
        var crate = AddCrate();
        var command = EntityLifetimeCommand.ForDeleted(Context, crate, "Crate"); // snapshot taken now...
        _world.Get<Transform>(crate).Position.X = 55f;                            // ...then the entity changes before the delete runs
        _stack.Execute(command);

        _stack.Undo();

        Assert.Equal(55f, _world.Get<Transform>(_world.Entities.Single()).Position.X);
    }

    [Fact]
    public void A_full_edit_session_can_be_undone_back_to_the_start()
    {
        var crate = AddCrate();
        object before = Boxed(crate, typeof(Transform));
        _world.Get<Transform>(crate).Position.Y = -5f;
        _stack.Push(new SetComponentCommand(Context, crate, typeof(Transform), before, Boxed(crate, typeof(Transform)), "Crate"));
        _stack.Execute(new AddComponentCommand(Context, crate, typeof(BoxCollider), null, "Crate"));
        _stack.Execute(EntityLifetimeCommand.ForDeleted(Context, crate, "Crate"));

        while (_stack.CanUndo) _stack.Undo();

        var restored = _world.Entities.Single();
        Assert.Equal(20f, _world.Get<Transform>(restored).Position.Y);
        Assert.False(_world.Has<BoxCollider>(restored));
        Assert.Equal("Crate", _world.Get<Name>(restored).Value);
    }

    [Fact]
    public void A_snapshot_restores_a_copy_not_the_original()
    {
        var crate = AddCrate();
        var snapshot = EntitySnapshot.Capture(_world, crate);

        var copy = snapshot.Restore(_world);

        Assert.NotEqual(crate, copy);
        Assert.Equal(2, snapshot.Count);
        Assert.Equal(2, _world.EntityCount);
    }
}
