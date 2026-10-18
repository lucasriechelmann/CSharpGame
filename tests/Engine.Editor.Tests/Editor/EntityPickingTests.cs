using System.Numerics;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Rendering;
using Engine.Editor;
using Engine.Tilemaps;

namespace Engine.Editor.Tests.Editor;

public class EntityPickingTests
{
    private readonly World _world = new();

    private Entity AddSprite(float x, float y, float z, float width, float height, Vector2? pivot = null, float rotation = 0f, Vector2? scale = null)
    {
        var entity = _world.CreateEntity();
        var transform = Transform.At(x, y, z);
        transform.SetRotationZ(rotation);
        if (scale is { } s) transform.Scale = new Vector3(s, 1f);
        _world.Add(entity, transform);
        var sprite = Sprite.Create(AssetHandle<Texture2D>.Invalid, new Vector2(width, height));
        if (pivot is { } p) sprite.Pivot = p;
        _world.Add(entity, sprite);
        return entity;
    }

    private Entity AddMarker(float x, float y, float z = 0f)
    {
        var entity = _world.CreateEntity();
        _world.Add(entity, Transform.At(x, y, z));
        return entity;
    }

    [Fact]
    public void A_point_inside_a_centred_sprite_picks_it()
    {
        var sprite = AddSprite(100f, 50f, 0f, 40f, 20f);

        Assert.Equal(sprite, EntityPicking.Pick(_world, new Vector2(115f, 55f), 10f));
    }

    [Fact]
    public void A_point_outside_every_sprite_picks_nothing()
    {
        AddSprite(100f, 50f, 0f, 40f, 20f);

        Assert.True(EntityPicking.Pick(_world, new Vector2(125f, 50f), 10f).IsNull);
    }

    [Fact]
    public void An_empty_world_picks_nothing()
    {
        Assert.True(EntityPicking.Pick(_world, Vector2.Zero, 10f).IsNull);
    }

    [Fact]
    public void The_pivot_decides_where_the_quad_sits()
    {
        // Pivot (0, 0) = top-left corner on the position, so the quad extends right and DOWN from it.
        var sprite = AddSprite(0f, 0f, 0f, 20f, 10f, pivot: new Vector2(0f, 0f));

        Assert.Equal(sprite, EntityPicking.Pick(_world, new Vector2(15f, -5f), 1f));
        Assert.True(EntityPicking.Pick(_world, new Vector2(15f, 5f), 1f).IsNull);
        Assert.True(EntityPicking.Pick(_world, new Vector2(-5f, -5f), 1f).IsNull);
    }

    [Fact]
    public void Transform_scale_grows_the_pickable_area()
    {
        var sprite = AddSprite(0f, 0f, 0f, 10f, 10f, scale: new Vector2(3f, 1f));

        Assert.Equal(sprite, EntityPicking.Pick(_world, new Vector2(14f, 0f), 1f));
    }

    [Fact]
    public void Rotation_turns_the_pickable_area()
    {
        // A 40x10 bar turned a quarter turn is 10 wide and 40 tall.
        var bar = AddSprite(0f, 0f, 0f, 40f, 10f, rotation: MathF.PI / 2f);

        Assert.Equal(bar, EntityPicking.Pick(_world, new Vector2(0f, 15f), 1f));
        Assert.True(EntityPicking.Pick(_world, new Vector2(15f, 0f), 1f).IsNull);
    }

    [Fact]
    public void The_sprite_in_front_wins_where_two_overlap()
    {
        AddSprite(0f, 0f, 1f, 40f, 40f);
        var front = AddSprite(0f, 0f, 5f, 40f, 40f);

        Assert.Equal(front, EntityPicking.Pick(_world, Vector2.Zero, 1f));
    }

    [Fact]
    public void At_equal_depth_the_smaller_sprite_wins()
    {
        AddSprite(0f, 0f, 3f, 100f, 100f);
        var small = AddSprite(0f, 0f, 3f, 10f, 10f);

        Assert.Equal(small, EntityPicking.Pick(_world, Vector2.Zero, 1f));
    }

    [Fact]
    public void An_entity_without_a_sprite_is_picked_within_the_fallback_radius()
    {
        var light = AddMarker(10f, 10f);

        Assert.Equal(light, EntityPicking.Pick(_world, new Vector2(15f, 10f), 8f));
        Assert.True(EntityPicking.Pick(_world, new Vector2(30f, 10f), 8f).IsNull);
    }

    [Fact]
    public void A_marker_in_front_of_a_sprite_is_picked_over_it()
    {
        AddSprite(0f, 0f, 1f, 100f, 100f);
        var marker = AddMarker(0f, 0f, 9f);

        Assert.Equal(marker, EntityPicking.Pick(_world, Vector2.Zero, 8f));
    }

    [Fact]
    public void Tile_layers_are_never_picked()
    {
        var layer = _world.CreateEntity();
        _world.Add(layer, Transform.At(0f, 0f));
        _world.Add(layer, new TileLayerRenderer());

        Assert.True(EntityPicking.Pick(_world, Vector2.Zero, 50f).IsNull);
    }

    [Fact]
    public void Entities_without_a_transform_are_ignored()
    {
        var entity = _world.CreateEntity();
        _world.Add(entity, Sprite.Create(AssetHandle<Texture2D>.Invalid, new Vector2(100f, 100f)));

        Assert.True(EntityPicking.Pick(_world, Vector2.Zero, 50f).IsNull);
    }

    [Fact]
    public void The_outline_of_an_unrotated_sprite_is_its_rectangle()
    {
        var sprite = AddSprite(100f, 50f, 0f, 40f, 20f);
        Span<Vector2> corners = stackalloc Vector2[4];

        Assert.True(EntityPicking.TryGetOutline(_world, sprite, corners));

        Assert.Equal(new Vector2(80f, 40f), corners[0]);
        Assert.Equal(new Vector2(120f, 40f), corners[1]);
        Assert.Equal(new Vector2(120f, 60f), corners[2]);
        Assert.Equal(new Vector2(80f, 60f), corners[3]);
    }

    [Fact]
    public void The_outline_follows_rotation()
    {
        var bar = AddSprite(0f, 0f, 0f, 40f, 10f, rotation: MathF.PI / 2f);
        Span<Vector2> corners = stackalloc Vector2[4];

        EntityPicking.TryGetOutline(_world, bar, corners);

        foreach (var corner in corners)
        {
            Assert.InRange(MathF.Abs(corner.X), 4.99f, 5.01f);
            Assert.InRange(MathF.Abs(corner.Y), 19.99f, 20.01f);
        }
    }

    [Fact]
    public void An_entity_without_a_sprite_has_no_outline()
    {
        var marker = AddMarker(0f, 0f);

        Assert.False(EntityPicking.TryGetOutline(_world, marker, stackalloc Vector2[4]));
    }

    [Fact]
    public void Asking_for_an_outline_with_too_few_corners_is_rejected()
    {
        var sprite = AddSprite(0f, 0f, 0f, 1f, 1f);

        Assert.Throws<ArgumentException>(() => EntityPicking.TryGetOutline(_world, sprite, new Vector2[3]));
    }

    [Fact]
    public void A_child_sprite_is_picked_where_its_parent_puts_it()
    {
        var parent = AddMarker(200f, 0f);
        var child = AddSprite(10f, 0f, 0f, 20f, 20f);
        Engine.Core.Hierarchy.TransformHierarchy.SetParent(_world, child, parent, keepWorld: false);

        Assert.Equal(child, EntityPicking.Pick(_world, new Vector2(210f, 0f), 2f));
        Assert.Equal(Entity.Null, EntityPicking.Pick(_world, new Vector2(10f, 0f), 2f)); // its local position is not where it is drawn
    }
}
