using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;
using Engine.Rendering3D;
using Engine.Editor;

namespace Engine.Editor.Tests.Editor;

public class EntityPicking3DTests
{
    private readonly World _world = new();
    private readonly Camera3D _camera = new() { Position = new Vector3(0, 0, 10), ViewportSize = new Vector2(800, 600) };
    private readonly Dictionary<Entity, PickBox> _boxes = new();

    private Entity AddBox(Vector3 position, Vector3 size, float yaw = 0f, Vector3? scale = null)
    {
        var entity = _world.CreateEntity();
        var transform = Transform.At(position);
        transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw);
        if (scale is { } s) transform.Scale = s;
        _world.Add(entity, transform);
        _boxes[entity] = new PickBox(TransformHierarchy.GetWorldMatrix(_world, entity), -size * 0.5f, size * 0.5f);
        return entity;
    }

    private Entity AddMarker(Vector3 position)
    {
        var entity = _world.CreateEntity();
        _world.Add(entity, Transform.At(position));
        return entity;
    }

    private void Source(Entity entity, List<PickBox> boxes)
    {
        if (_boxes.TryGetValue(entity, out var box)) boxes.Add(box);
    }

    private Entity Pick(Ray ray, out float distance) => EntityPicking3D.Pick(_world, ray, _camera, Source, out distance);

    private static Ray AlongMinusZ(float x = 0f, float y = 0f) => new(new Vector3(x, y, 10f), -Vector3.UnitZ);

    [Fact]
    public void A_ray_through_a_box_picks_it_and_reports_the_distance_to_its_front_face()
    {
        var box = AddBox(Vector3.Zero, new Vector3(2, 2, 2));

        var hit = Pick(AlongMinusZ(), out float distance);

        Assert.Equal(box, hit);
        Assert.Equal(9f, distance, 4); // eye at z=10, front face at z=1
    }

    [Fact]
    public void A_ray_that_misses_picks_nothing()
    {
        AddBox(Vector3.Zero, new Vector3(2, 2, 2));

        Assert.Equal(Entity.Null, Pick(AlongMinusZ(x: 5f), out _));
    }

    [Fact]
    public void The_nearest_of_two_boxes_on_the_ray_wins()
    {
        AddBox(new Vector3(0, 0, -5), new Vector3(2, 2, 2));
        var near = AddBox(new Vector3(0, 0, 3), new Vector3(2, 2, 2));

        Assert.Equal(near, Pick(AlongMinusZ(), out _));
    }

    [Fact]
    public void A_box_behind_the_ray_origin_is_not_hit()
    {
        AddBox(new Vector3(0, 0, 20), new Vector3(2, 2, 2));

        Assert.Equal(Entity.Null, Pick(AlongMinusZ(), out _));
    }

    [Fact]
    public void A_rotated_box_is_hit_where_the_box_is_not_where_its_world_axis_bounds_are()
    {
        // A long thin box turned a quarter turn about Y now lies along Z instead of X.
        var box = AddBox(Vector3.Zero, new Vector3(10, 1, 1), yaw: MathF.PI / 2f);

        Assert.Equal(Entity.Null, Pick(AlongMinusZ(x: 4f), out _)); // the unrotated box would have covered x = 4
        Assert.Equal(box, Pick(AlongMinusZ(x: 0.2f), out _));
    }

    [Fact]
    public void A_scaled_box_is_hit_over_its_scaled_size_and_distance_stays_in_world_units()
    {
        var box = AddBox(Vector3.Zero, Vector3.One, scale: new Vector3(4, 4, 4));

        var hit = Pick(AlongMinusZ(x: 1.5f), out float distance);

        Assert.Equal(box, hit);
        Assert.Equal(8f, distance, 4); // front face at z = 2
    }

    [Fact]
    public void An_entity_without_a_box_is_hit_near_its_position_and_missed_far_from_it()
    {
        var marker = AddMarker(new Vector3(0, 0, 0));

        Assert.Equal(marker, Pick(AlongMinusZ(x: 0.05f), out _));
        Assert.Equal(Entity.Null, Pick(AlongMinusZ(x: 3f), out _));
    }

    [Fact]
    public void The_no_volume_radius_stays_about_the_same_size_on_screen()
    {
        float near = EntityPicking3D.WorldUnitsPerPixel(_camera, new Vector3(0, 0, 5)) * EntityPicking3D.FallbackRadiusPixels;
        float far = EntityPicking3D.WorldUnitsPerPixel(_camera, new Vector3(0, 0, -40)) * EntityPicking3D.FallbackRadiusPixels;

        Assert.True(far > near * 5f); // 45 units away spans far more world per pixel than 5 units away
    }

    [Fact]
    public void A_child_box_follows_its_parent()
    {
        var parent = AddMarker(new Vector3(20, 0, 0));
        var child = AddBox(Vector3.Zero, new Vector3(2, 2, 2));
        TransformHierarchy.SetParent(_world, child, parent, keepWorld: false);
        _boxes[child] = new PickBox(TransformHierarchy.GetWorldMatrix(_world, child), new Vector3(-1), new Vector3(1));

        Assert.Equal(child, Pick(AlongMinusZ(x: 20f), out _));
        Assert.NotEqual(child, Pick(AlongMinusZ(x: 0f), out _));
    }

    [Fact]
    public void A_box_with_no_volume_is_never_hit()
    {
        AddBox(Vector3.Zero, new Vector3(2, 2, 2), scale: new Vector3(0, 1, 1));

        Assert.Equal(Entity.Null, Pick(AlongMinusZ(), out _)); // singular matrix: nothing to hit, and it is not mistaken for an entity without a box
    }

    [Fact]
    public void Corners_are_placed_by_the_boxes_matrix()
    {
        var box = new PickBox(Matrix4x4.CreateTranslation(10, 0, 0), new Vector3(-1), new Vector3(1));
        Span<Vector3> corners = stackalloc Vector3[8];

        EntityPicking3D.GetCorners(box, corners);

        Assert.Equal(new Vector3(9, -1, -1), corners[0]);
        Assert.Equal(new Vector3(11, 1, 1), corners[6]);
    }

    [Fact]
    public void Fewer_than_eight_corner_slots_is_refused()
    {
        var box = new PickBox(Matrix4x4.Identity, Vector3.Zero, Vector3.One);

        Assert.Throws<ArgumentException>(() => EntityPicking3D.GetCorners(box, new Vector3[7]));
    }
}
