using Engine.Core.Ecs;
using Engine.Core.Hierarchy;
using Engine.Runtime.Editing;

namespace Engine.Editor.Tests.Editor;

public class EditorSelectionTests
{
    private readonly World _world = new();
    private readonly EditorSelection _selection = new();

    [Fact]
    public void A_new_selection_is_empty()
    {
        Assert.Equal(0, _selection.Count);
        Assert.Equal(Entity.Null, _selection.Primary);
    }

    [Fact]
    public void Selecting_one_entity_replaces_everything_else()
    {
        var a = _world.CreateEntity();
        var b = _world.CreateEntity();
        _selection.Add(a);

        _selection.SetOnly(b);

        Assert.Equal([b], _selection.Items);
        Assert.Equal(b, _selection.Primary);
    }

    [Fact]
    public void Selecting_the_null_entity_clears()
    {
        _selection.Add(_world.CreateEntity());

        _selection.SetOnly(Entity.Null);

        Assert.Equal(0, _selection.Count);
    }

    [Fact]
    public void Toggling_adds_then_removes_and_the_latest_addition_is_primary()
    {
        var a = _world.CreateEntity();
        var b = _world.CreateEntity();
        _selection.Toggle(a);
        _selection.Toggle(b);

        Assert.Equal(b, _selection.Primary);
        Assert.True(_selection.Contains(a));

        _selection.Toggle(b);

        Assert.Equal([a], _selection.Items);
        Assert.Equal(a, _selection.Primary);
    }

    [Fact]
    public void Toggling_or_adding_the_null_entity_does_nothing()
    {
        _selection.Toggle(Entity.Null);
        _selection.Add(Entity.Null);

        Assert.Equal(0, _selection.Count);
    }

    [Fact]
    public void Adding_an_already_selected_entity_makes_it_primary_without_duplicating_it()
    {
        var a = _world.CreateEntity();
        var b = _world.CreateEntity();
        _selection.Add(a);
        _selection.Add(b);

        _selection.Add(a);

        Assert.Equal([b, a], _selection.Items);
    }

    [Fact]
    public void Pruning_drops_destroyed_entities()
    {
        var a = _world.CreateEntity();
        var b = _world.CreateEntity();
        _selection.Add(a);
        _selection.Add(b);
        _world.DestroyEntity(b);

        _selection.Prune(_world);

        Assert.Equal([a], _selection.Items);
        Assert.Equal(a, _selection.Primary);
    }

    [Fact]
    public void Top_level_skips_entities_whose_ancestor_is_also_selected()
    {
        var root = _world.CreateEntity();
        var child = _world.CreateEntity();
        var grandchild = _world.CreateEntity();
        var other = _world.CreateEntity();
        TransformHierarchy.SetParent(_world, child, root, keepWorld: false);
        TransformHierarchy.SetParent(_world, grandchild, child, keepWorld: false);
        _selection.Add(grandchild);
        _selection.Add(root);
        _selection.Add(other);
        var top = new List<Entity>();

        _selection.GetTopLevel(_world, top);

        Assert.Equal([root, other], top);
    }

    [Fact]
    public void A_selected_child_of_an_unselected_parent_is_top_level()
    {
        var parent = _world.CreateEntity();
        var child = _world.CreateEntity();
        TransformHierarchy.SetParent(_world, child, parent, keepWorld: false);
        _selection.Add(child);
        var top = new List<Entity>();

        _selection.GetTopLevel(_world, top);

        Assert.Equal([child], top);
    }
}
