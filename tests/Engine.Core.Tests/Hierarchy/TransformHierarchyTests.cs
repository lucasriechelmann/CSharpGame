using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;

namespace Engine.Core.Tests.Hierarchy;

public class TransformHierarchyTests
{
    private static Entity Make(World world, Vector3 position, float scale = 1f)
    {
        var entity = world.CreateEntity();
        var transform = Transform.At(position);
        transform.Scale = new Vector3(scale);
        world.Add(entity, transform);
        return entity;
    }

    [Fact]
    public void Root_world_transform_is_its_own_transform()
    {
        var world = new World();
        var root = Make(world, new Vector3(3, 4, 5));

        var result = TransformHierarchy.GetWorldTransform(world, root);

        Assert.Equal(new Vector3(3, 4, 5), result.Position);
    }

    [Fact]
    public void Child_position_is_relative_to_its_parent()
    {
        var world = new World();
        var parent = Make(world, new Vector3(10, 0, 0));
        var child = Make(world, new Vector3(2, 1, 0));
        TransformHierarchy.SetParent(world, child, parent, keepWorld: false);

        var result = TransformHierarchy.GetWorldTransform(world, child);

        Assert.Equal(new Vector3(12, 1, 0), result.Position);
    }

    [Fact]
    public void Parent_scale_scales_the_childs_offset_and_size()
    {
        var world = new World();
        var parent = Make(world, Vector3.Zero, scale: 2f);
        var child = Make(world, new Vector3(1, 0, 0));
        TransformHierarchy.SetParent(world, child, parent, keepWorld: false);

        var result = TransformHierarchy.GetWorldTransform(world, child);

        Assert.Equal(new Vector3(2, 0, 0), result.Position);
        Assert.Equal(new Vector3(2, 2, 2), result.Scale);
    }

    [Fact]
    public void Parent_rotation_swings_the_child_around_it()
    {
        var world = new World();
        var parent = Make(world, new Vector3(5, 0, 0));
        world.Get<Transform>(parent).SetRotationZ(MathF.PI / 2f);
        var child = Make(world, new Vector3(1, 0, 0));
        TransformHierarchy.SetParent(world, child, parent, keepWorld: false);

        var result = TransformHierarchy.GetWorldTransform(world, child);

        Assert.Equal(5f, result.Position.X, 4);
        Assert.Equal(1f, result.Position.Y, 4);
    }

    [Fact]
    public void Three_levels_compose_in_order()
    {
        var world = new World();
        var a = Make(world, new Vector3(1, 0, 0));
        var b = Make(world, new Vector3(0, 2, 0));
        var c = Make(world, new Vector3(0, 0, 3));
        TransformHierarchy.SetParent(world, b, a, keepWorld: false);
        TransformHierarchy.SetParent(world, c, b, keepWorld: false);

        Assert.Equal(new Vector3(1, 2, 3), TransformHierarchy.GetWorldTransform(world, c).Position);
    }

    [Fact]
    public void Reparenting_with_keepWorld_leaves_the_world_position_unchanged()
    {
        var world = new World();
        var parent = Make(world, new Vector3(10, 10, 0), scale: 2f);
        var child = Make(world, new Vector3(30, 20, 0));

        TransformHierarchy.SetParent(world, child, parent, keepWorld: true);

        var local = world.Get<Transform>(child);
        Assert.Equal(10f, local.Position.X, 4);
        Assert.Equal(5f, local.Position.Y, 4);
        Assert.Equal(0.5f, local.Scale.X, 4);
        Assert.Equal(new Vector3(30, 20, 0).X, TransformHierarchy.GetWorldTransform(world, child).Position.X, 4);
    }

    [Fact]
    public void Detaching_with_keepWorld_bakes_the_world_placement_into_the_transform()
    {
        var world = new World();
        var parent = Make(world, new Vector3(10, 0, 0));
        var child = Make(world, new Vector3(2, 0, 0));
        TransformHierarchy.SetParent(world, child, parent, keepWorld: false);

        TransformHierarchy.SetParent(world, child, Entity.Null, keepWorld: true);

        Assert.False(world.Has<Parent>(child));
        Assert.Equal(new Vector3(12, 0, 0), world.Get<Transform>(child).Position);
    }

    [Fact]
    public void Making_an_entity_its_own_descendant_is_refused()
    {
        var world = new World();
        var a = Make(world, Vector3.Zero);
        var b = Make(world, Vector3.Zero);
        TransformHierarchy.SetParent(world, b, a, keepWorld: false);

        Assert.Throws<InvalidOperationException>(() => TransformHierarchy.SetParent(world, a, b, keepWorld: false));
        Assert.Throws<InvalidOperationException>(() => TransformHierarchy.SetParent(world, a, a, keepWorld: false));
    }

    [Fact]
    public void A_dead_parent_makes_the_child_a_root()
    {
        var world = new World();
        var parent = Make(world, new Vector3(10, 0, 0));
        var child = Make(world, new Vector3(2, 0, 0));
        TransformHierarchy.SetParent(world, child, parent, keepWorld: false);

        world.DestroyEntity(parent);

        Assert.False(TransformHierarchy.HasParent(world, child));
        Assert.Equal(new Vector3(2, 0, 0), TransformHierarchy.GetWorldTransform(world, child).Position);
    }

    [Fact]
    public void Destroy_removes_the_whole_subtree_but_not_siblings()
    {
        var world = new World();
        var root = Make(world, Vector3.Zero);
        var child = Make(world, Vector3.Zero);
        var grandchild = Make(world, Vector3.Zero);
        var sibling = Make(world, Vector3.Zero);
        TransformHierarchy.SetParent(world, child, root, keepWorld: false);
        TransformHierarchy.SetParent(world, grandchild, child, keepWorld: false);

        TransformHierarchy.Destroy(world, root);

        Assert.False(world.IsAlive(root));
        Assert.False(world.IsAlive(child));
        Assert.False(world.IsAlive(grandchild));
        Assert.True(world.IsAlive(sibling));
    }

    [Fact]
    public void Subtree_lists_parents_before_children()
    {
        var world = new World();
        var root = Make(world, Vector3.Zero);
        var a = Make(world, Vector3.Zero);
        var b = Make(world, Vector3.Zero);
        var a1 = Make(world, Vector3.Zero);
        TransformHierarchy.SetParent(world, a, root, keepWorld: false);
        TransformHierarchy.SetParent(world, b, root, keepWorld: false);
        TransformHierarchy.SetParent(world, a1, a, keepWorld: false);

        var subtree = new List<Entity>();
        TransformHierarchy.GetSubtree(world, root, subtree);

        Assert.Equal([root, a, b, a1], subtree);
    }

    [Fact]
    public void Render_transform_blends_every_level_of_the_chain()
    {
        var world = new World();
        var parent = Make(world, new Vector3(10, 0, 0));
        world.Add(parent, new PreviousTransform { Position = new Vector3(0, 0, 0), Rotation = Quaternion.Identity });
        var child = Make(world, new Vector3(1, 0, 0));
        TransformHierarchy.SetParent(world, child, parent, keepWorld: false);
        var clock = new RenderClock { Alpha = 0.5f };

        var result = TransformHierarchy.GetRenderTransform(world, child, world.Get<Transform>(child), clock);

        Assert.Equal(6f, result.Position.X, 4); // parent drawn half way (5) plus the child's offset (1)
    }

    [Fact]
    public void Render_transform_of_a_root_is_the_interpolated_own_transform()
    {
        var world = new World();
        var root = Make(world, new Vector3(8, 0, 0));
        world.Add(root, new PreviousTransform { Position = Vector3.Zero, Rotation = Quaternion.Identity });
        var clock = new RenderClock { Alpha = 0.25f };

        var result = TransformHierarchy.GetRenderTransform(world, root, world.Get<Transform>(root), clock);

        Assert.Equal(2f, result.Position.X, 4);
    }

    [Fact]
    public void FromMatrix_round_trips_ToMatrix()
    {
        var original = Transform.At(1, 2, 3);
        original.Scale = new Vector3(2, 3, 4);
        original.SetRotationZ(0.5f);

        var back = Transform.FromMatrix(original.ToMatrix());

        Assert.Equal(original.Position.X, back.Position.X, 4);
        Assert.Equal(original.Scale.Y, back.Scale.Y, 4);
        Assert.Equal(original.RotationZ, back.RotationZ, 4);
    }
}
