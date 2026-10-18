using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;
using Engine.Editor.Gizmos;

namespace Engine.Editor.Tests.Editor.Gizmos;

public class GizmoApplyTests
{
    private static GizmoStart RootStart(Transform transform) => new(transform, transform, false, Matrix4x4.Identity);

    private static GizmoStart StartOf(World world, Entity entity)
    {
        var parent = TransformHierarchy.GetParent(world, entity);
        return new GizmoStart(
            world.Get<Transform>(entity),
            TransformHierarchy.GetWorldTransform(world, entity),
            !parent.IsNull,
            parent.IsNull ? Matrix4x4.Identity : TransformHierarchy.GetWorldMatrix(world, parent));
    }

    private static GizmoDelta Move(Vector3 by) => new(by, Quaternion.Identity, Vector3.One);

    private static GizmoDelta Turn(Vector3 axis, float angle) => new(Vector3.Zero, Quaternion.CreateFromAxisAngle(axis, angle), Vector3.One);

    [Fact]
    public void Translating_a_root_adds_the_offset()
    {
        var start = RootStart(Transform.At(1, 2, 3));

        var result = GizmoApply.Apply(GizmoMode.Translate, start, Move(new Vector3(10, 0, -1)), Vector3.Zero);

        Assert.Equal(new Vector3(11, 2, 2), result.Position);
        Assert.Equal(start.Local.Rotation, result.Rotation);
        Assert.Equal(start.Local.Scale, result.Scale);
    }

    [Fact]
    public void Translating_a_child_moves_it_by_the_same_world_distance_through_a_scaled_parent()
    {
        var world = new World();
        var parent = world.CreateEntity();
        var parentTransform = Transform.At(100, 0, 0);
        parentTransform.Scale = new Vector3(2f);
        world.Add(parent, parentTransform);
        var child = world.CreateEntity();
        world.Add(child, Transform.At(5, 0, 0));
        TransformHierarchy.SetParent(world, child, parent, keepWorld: false);
        var start = StartOf(world, child); // world position 110

        var result = GizmoApply.Apply(GizmoMode.Translate, start, Move(new Vector3(20, 0, 0)), Vector3.Zero);
        world.Get<Transform>(child) = result;

        Assert.Equal(130f, TransformHierarchy.GetWorldTransform(world, child).Position.X, 3);
        Assert.Equal(15f, result.Position.X, 3); // 20 world units are 10 local units under a parent scaled by two
    }

    [Fact]
    public void Rotating_about_the_entitys_own_position_changes_only_its_orientation()
    {
        var start = RootStart(Transform.At(4, 5, 0));

        var result = GizmoApply.Apply(GizmoMode.Rotate, start, Turn(Vector3.UnitZ, MathF.PI / 2f), pivot: new Vector3(4, 5, 0));

        Assert.Equal(4f, result.Position.X, 4);
        Assert.Equal(5f, result.Position.Y, 4);
        Assert.Equal(MathF.PI / 2f, result.RotationZ, 4);
    }

    [Fact]
    public void Rotating_about_another_pivot_swings_the_position_round_it()
    {
        var start = RootStart(Transform.At(10, 0, 0));

        var result = GizmoApply.Apply(GizmoMode.Rotate, start, Turn(Vector3.UnitZ, MathF.PI / 2f), pivot: Vector3.Zero);

        Assert.Equal(0f, result.Position.X, 4);
        Assert.Equal(10f, result.Position.Y, 4);
    }

    [Fact]
    public void A_world_axis_turn_comes_after_the_entitys_existing_rotation()
    {
        var turnedAboutY = Transform.Identity;
        turnedAboutY.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f);
        var start = RootStart(turnedAboutY);

        var result = GizmoApply.Apply(GizmoMode.Rotate, start, Turn(Vector3.UnitX, MathF.PI / 2f), Vector3.Zero);

        // The entity's +Z first goes to +X (turn about Y), then the world X turn leaves +X alone.
        var forward = Vector3.Transform(Vector3.UnitZ, result.Rotation);
        Assert.Equal(1f, forward.X, 4);
        Assert.Equal(0f, forward.Y, 4);
    }

    [Fact]
    public void Rotating_a_child_turns_it_in_world_space_whatever_its_parent_does()
    {
        var world = new World();
        var parent = world.CreateEntity();
        var parentTransform = Transform.Identity;
        parentTransform.SetRotationZ(MathF.PI / 2f);
        world.Add(parent, parentTransform);
        var child = world.CreateEntity();
        world.Add(child, Transform.Identity);
        TransformHierarchy.SetParent(world, child, parent, keepWorld: false);
        var start = StartOf(world, child); // world orientation: a quarter turn, from the parent

        var result = GizmoApply.Apply(GizmoMode.Rotate, start, Turn(Vector3.UnitZ, MathF.PI / 2f), start.World.Position);
        world.Get<Transform>(child) = result;

        Assert.Equal(MathF.PI, MathF.Abs(TransformHierarchy.GetWorldTransform(world, child).RotationZ), 3); // a half turn in the world
        Assert.Equal(MathF.PI / 2f, result.RotationZ, 3); // so the child's own part is the other quarter
    }

    [Fact]
    public void Scaling_multiplies_each_local_axis()
    {
        var transform = Transform.Identity;
        transform.Scale = new Vector3(2, 3, 4);
        var start = RootStart(transform);

        var result = GizmoApply.Apply(GizmoMode.Scale, start, new GizmoDelta(Vector3.Zero, Quaternion.Identity, new Vector3(2, 1, 0.5f)), Vector3.Zero);

        Assert.Equal(new Vector3(4, 3, 2), result.Scale);
        Assert.Equal(transform.Position, result.Position);
    }

    [Fact]
    public void Applying_the_same_total_delta_twice_gives_the_same_result_because_it_starts_from_the_start()
    {
        var start = RootStart(Transform.At(1, 1, 1));
        var delta = Move(new Vector3(3, 0, 0));

        var first = GizmoApply.Apply(GizmoMode.Translate, start, delta, Vector3.Zero);
        var second = GizmoApply.Apply(GizmoMode.Translate, start, delta, Vector3.Zero);

        Assert.Equal(first, second);
        Assert.Equal(4f, second.Position.X);
    }

    [Fact]
    public void A_parent_with_zero_scale_is_reported_not_silently_ignored()
    {
        var local = Transform.Identity;
        var start = new GizmoStart(local, local, true, Matrix4x4.CreateScale(0f));

        Assert.Throws<InvalidOperationException>(() => GizmoApply.Apply(GizmoMode.Translate, start, Move(Vector3.One), Vector3.Zero));
    }
}
