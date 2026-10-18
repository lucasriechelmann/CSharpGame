using System.Numerics;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Editor;
using Engine.Physics3D;
using Engine.Rendering3D;

namespace Engine.Editor.Tests;

public class EntityFactory3DTests
{
    private readonly World _world = new();

    [Theory]
    [InlineData(0.24f, 0f)]
    [InlineData(0.26f, 0.5f)]
    [InlineData(-0.3f, -0.5f)]
    [InlineData(3.7f, 3.5f)]
    public void Snap_rounds_each_coordinate_to_the_grid(float value, float expected)
    {
        var snapped = EntityFactory3D.Snap(new Vector3(value, value, value), 0.5f);

        Assert.Equal(new Vector3(expected), snapped);
    }

    [Fact]
    public void A_zero_grid_leaves_the_point_alone()
    {
        Assert.Equal(new Vector3(0.123f, 4f, 5.678f), EntityFactory3D.Snap(new Vector3(0.123f, 4f, 5.678f), 0f));
    }

    [Fact]
    public void A_plain_box_is_drawn_but_has_no_collision()
    {
        var box = EntityFactory3D.Box(_world, "Crate", new Vector3(1f, 2f, 3f), new Vector3(2f, 1f, 4f), default, default, solid: false);

        Assert.Equal("Crate", _world.Get<Name>(box).Value);
        Assert.Equal(new Vector3(1f, 2f, 3f), _world.Get<Transform>(box).Position);
        Assert.Equal(new Vector3(2f, 1f, 4f), _world.Get<Transform>(box).Scale);
        Assert.True(_world.Has<MeshRenderer>(box));
        Assert.False(_world.Has<BoxCollider3D>(box));
        Assert.False(_world.Has<StaticSolid3D>(box));
    }

    [Fact]
    public void A_solid_box_gets_a_collider_the_size_of_its_scale_and_a_static_solid_marker()
    {
        var box = EntityFactory3D.Box(_world, "Platform", Vector3.Zero, new Vector3(3f, 0.5f, 3f), default, default, solid: true);

        Assert.Equal(new Vector3(3f, 0.5f, 3f), _world.Get<BoxCollider3D>(box).Size);
        Assert.True(_world.Has<StaticSolid3D>(box));
    }

    [Fact]
    public void The_given_mesh_and_material_handles_are_kept()
    {
        var mesh = new AssetHandle<Mesh>(7);
        var material = new AssetHandle<Material>(3);

        var box = EntityFactory3D.Box(_world, "Crate", Vector3.Zero, Vector3.One, mesh, material, solid: false);

        Assert.Equal(mesh, _world.Get<MeshRenderer>(box).Mesh);
        Assert.Equal(material, _world.Get<MeshRenderer>(box).Material);
    }

    [Fact]
    public void A_point_light_has_a_position_and_reach()
    {
        var light = EntityFactory3D.PointLight(_world, new Vector3(0f, 2f, 0f), radius: 6f);

        Assert.Equal(6f, _world.Get<PointLight3D>(light).Radius);
        Assert.Equal(new Vector3(0f, 2f, 0f), _world.Get<Transform>(light).Position);
    }
}
