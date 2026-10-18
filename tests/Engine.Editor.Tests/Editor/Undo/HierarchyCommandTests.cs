using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;
using Engine.Editor;
using Engine.Runtime.Editing;

namespace Engine.Editor.Tests.Editor.Undo;

public class HierarchyCommandTests
{
    private readonly World _world = new();
    private readonly CommandStack _stack;

    public HierarchyCommandTests() => _stack = new CommandStack(new EditContext(_world));

    private EditContext Context => _stack.Context;

    private Entity Make(string name, float x, float y)
    {
        var entity = _world.CreateEntity();
        _world.Add(entity, Name.Of(name));
        _world.Add(entity, Transform.At(x, y));
        return entity;
    }

    private Vector3 WorldPosition(Entity entity) => TransformHierarchy.GetWorldTransform(_world, entity).Position;

    // ---- ReparentCommand ----

    [Fact]
    public void Reparenting_keeps_the_world_position_and_undo_restores_the_exact_old_transform()
    {
        var parent = Make("parent", 100f, 0f);
        var child = Make("child", 130f, 20f);

        _stack.Execute(ReparentCommand.Create(Context, child, parent, "child")!);

        Assert.Equal(parent, TransformHierarchy.GetParent(_world, child));
        Assert.Equal(30f, _world.Get<Transform>(child).Position.X, 4);
        Assert.Equal(130f, WorldPosition(child).X, 4);

        _stack.Undo();

        Assert.False(TransformHierarchy.HasParent(_world, child));
        Assert.Equal(130f, _world.Get<Transform>(child).Position.X);

        _stack.Redo();
        Assert.Equal(parent, TransformHierarchy.GetParent(_world, child));
    }

    [Fact]
    public void Detaching_with_the_command_bakes_the_world_position_in()
    {
        var parent = Make("parent", 100f, 0f);
        var child = Make("child", 5f, 0f);
        TransformHierarchy.SetParent(_world, child, parent, keepWorld: false);

        _stack.Execute(ReparentCommand.Create(Context, child, Entity.Null, "child")!);

        Assert.False(TransformHierarchy.HasParent(_world, child));
        Assert.Equal(105f, _world.Get<Transform>(child).Position.X, 4);
    }

    [Fact]
    public void Reparenting_to_the_current_parent_is_a_no_op()
    {
        var parent = Make("parent", 0f, 0f);
        var child = Make("child", 0f, 0f);
        TransformHierarchy.SetParent(_world, child, parent, keepWorld: false);

        Assert.Null(ReparentCommand.Create(Context, child, parent, "child"));
    }

    [Fact]
    public void Reparenting_under_a_descendant_is_refused()
    {
        var parent = Make("parent", 0f, 0f);
        var child = Make("child", 0f, 0f);
        TransformHierarchy.SetParent(_world, child, parent, keepWorld: false);

        Assert.Throws<InvalidOperationException>(() => ReparentCommand.Create(Context, parent, child, "parent"));
    }

    // ---- snapshots with parents ----

    [Fact]
    public void A_deleted_child_comes_back_under_its_parent()
    {
        var parent = Make("parent", 10f, 0f);
        var child = Make("child", 2f, 0f);
        TransformHierarchy.SetParent(_world, child, parent, keepWorld: false);

        _stack.Execute(EntityLifetimeCommand.ForDeleted(Context, child, "child"));
        Assert.Equal(1, _world.EntityCount);

        _stack.Undo();

        var restored = Context.Resolve(Context.Entities.IdOf(Context.Entities.Lookup(1)));
        Assert.Equal(parent, TransformHierarchy.GetParent(_world, restored));
        Assert.Equal(12f, WorldPosition(restored).X, 4);
    }

    [Fact]
    public void Deleting_a_subtree_as_one_step_undoes_with_every_link_intact()
    {
        var root = Make("root", 0f, 0f);
        var child = Make("child", 1f, 0f);
        var grandchild = Make("grandchild", 1f, 0f);
        TransformHierarchy.SetParent(_world, child, root, keepWorld: false);
        TransformHierarchy.SetParent(_world, grandchild, child, keepWorld: false);

        var subtree = new List<Entity>();
        TransformHierarchy.GetSubtree(_world, root, subtree);
        var commands = new List<IEditCommand>();
        for (int i = subtree.Count - 1; i >= 0; i--) commands.Add(EntityLifetimeCommand.ForDeleted(Context, subtree[i], "x"));
        _stack.Execute(new CompositeCommand("Delete root", commands, commands.Count - 1));
        Assert.Equal(0, _world.EntityCount);
        Assert.Equal(1, _stack.UndoCount);

        var undone = _stack.Undo()!;

        Assert.Equal(3, _world.EntityCount);
        var restoredRoot = undone.Affected(Context);
        Assert.False(restoredRoot.IsNull);
        Assert.Equal("root", _world.Get<Name>(restoredRoot).Value);
        var restored = new List<Entity>();
        TransformHierarchy.GetSubtree(_world, restoredRoot, restored);
        Assert.Equal(3, restored.Count);
        Assert.Equal(2f, WorldPosition(restored[2]).X, 4); // grandchild: 0 + 1 + 1

        _stack.Redo();
        Assert.Equal(0, _world.EntityCount);
    }

    // ---- duplicating subtrees ----

    [Fact]
    public void Duplicating_a_subtree_copies_the_children_under_the_copy()
    {
        var root = Make("root", 0f, 0f);
        var child = Make("child", 3f, 0f);
        TransformHierarchy.SetParent(_world, child, root, keepWorld: false);

        var copies = EntityCloner.DuplicateSubtree(_world, root, new Vector2(50f, 0f));

        Assert.Equal(2, copies.Count);
        Assert.Equal(4, _world.EntityCount);
        Assert.Equal(copies[0], TransformHierarchy.GetParent(_world, copies[1]));
        Assert.Equal(53f, WorldPosition(copies[1]).X, 4);
        Assert.Equal("root (copy)", _world.Get<Name>(copies[0]).Value);
        Assert.Equal("child", _world.Get<Name>(copies[1]).Value);
        Assert.Equal(root, TransformHierarchy.GetParent(_world, child)); // the original is untouched
    }

    [Fact]
    public void Undoing_a_subtree_duplicate_removes_all_copies()
    {
        var root = Make("root", 0f, 0f);
        var child = Make("child", 3f, 0f);
        TransformHierarchy.SetParent(_world, child, root, keepWorld: false);
        var copies = EntityCloner.DuplicateSubtree(_world, root, Vector2.Zero);
        var commands = copies.Select(c => (IEditCommand)EntityLifetimeCommand.ForCreated(Context, c, "x")).ToList();
        _stack.Push(new CompositeCommand("Create root", commands, 0));

        _stack.Undo();
        Assert.Equal(2, _world.EntityCount);

        _stack.Redo();
        Assert.Equal(4, _world.EntityCount);
    }
}
