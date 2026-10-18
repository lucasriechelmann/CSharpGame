using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics3D;

namespace Engine.Physics3D.Tests;

public class OrientedSolidTests
{
    private const double Step = 1.0 / 64.0;

    private readonly World _world = new();
    private readonly PhysicsSettings3D _settings = new();
    private readonly PhysicsSystem3D _physics;

    public OrientedSolidTests() => _physics = new PhysicsSystem3D(_settings);

    private void AddFloor()
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(0f, -0.5f, 0f));
        _world.Add(e, BoxCollider3D.Create(new Vector3(100f, 1f, 100f)));
        _world.Add(e, new StaticSolid3D());
    }

    /// <summary>A 10 x 0.5 x 10 slab tilted about Z so it rises towards +X (its +X end is up) by <paramref name="degrees"/>, centred at (0, 2, 0).</summary>
    private Entity AddRamp(float degrees)
    {
        var e = _world.CreateEntity();
        var transform = Transform.At(0f, 2f, 0f);
        transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, degrees * MathF.PI / 180f);
        _world.Add(e, transform);
        _world.Add(e, new BoxCollider3D { Size = new Vector3(10f, 0.5f, 10f), Oriented = true });
        _world.Add(e, new StaticSolid3D());
        return e;
    }

    private Entity AddCrate(Vector3 position)
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(position));
        _world.Add(e, BoxCollider3D.Create(Vector3.One));
        _world.Add(e, PhysicsBody3D.Create());
        return e;
    }

    private Entity AddCapsule(Vector3 position)
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(position));
        _world.Add(e, CapsuleCollider3D.Create(0.4f, 1.8f));
        _world.Add(e, PhysicsBody3D.Create());
        return e;
    }

    private void Run(int steps)
    {
        for (int i = 0; i < steps; i++) _physics.Update(_world, Step);
    }

    private Vector3 PositionOf(Entity e) => _world.Get<Transform>(e).Position;

    [Fact]
    public void A_crate_dropped_on_a_flat_oriented_slab_rests_on_it()
    {
        AddFloor();
        AddRamp(0f); // a flat slab whose top face is at y = 2.25
        var crate = AddCrate(new Vector3(0f, 5f, 0f));

        Run(256);

        Assert.Equal(2.75f, PositionOf(crate).Y, 2);
        Assert.True(_world.Get<PhysicsBody3D>(crate).IsGrounded);
    }

    [Fact]
    public void A_crate_on_a_ramp_slides_down_it_instead_of_stopping_in_mid_air()
    {
        AddFloor();
        AddRamp(20f);
        _settings.GroundFriction = 0f;
        var crate = AddCrate(new Vector3(0f, 3.3f, 0f)); // just above the ramp's middle

        Run(32);
        float early = PositionOf(crate).X;
        Run(64);

        Assert.True(PositionOf(crate).X < early - 0.2f); // heading down the slope, towards -X
        Assert.True(PositionOf(crate).Y < 3.3f);
    }

    [Fact]
    public void A_crate_is_not_allowed_inside_a_ramp_while_it_slides()
    {
        AddFloor();
        var ramp = AddRamp(20f);
        _settings.GroundFriction = 0f;
        var crate = AddCrate(new Vector3(2f, 4.5f, 0f));

        for (int i = 0; i < 200; i++)
        {
            Run(1);
            var position = PositionOf(crate);
            var local = Quaternion.Inverse(_world.Get<Transform>(ramp).Rotation) is var inv
                ? Vector3.Transform(position - _world.Get<Transform>(ramp).Position, inv)
                : default;
            // The crate's centre must never be inside the ramp's own box.
            Assert.False(Math.Abs(local.X) < 5f && Math.Abs(local.Y) < 0.25f && Math.Abs(local.Z) < 5f, $"step {i}: centre inside the ramp at {local}");
        }
    }

    [Fact]
    public void A_capsule_standing_on_a_tilted_slab_is_held_up_by_it()
    {
        AddFloor();
        AddRamp(10f);
        var capsule = AddCapsule(new Vector3(0f, 4f, 0f));

        Run(256);

        // The slab's top at x = 0 is a little above y = 2.25 (tilted about its centre), so the capsule stands well above the flat floor.
        Assert.True(PositionOf(capsule).Y > 2.9f);
        Assert.True(_world.Get<PhysicsBody3D>(capsule).IsGrounded);
    }

    [Fact]
    public void A_capsule_on_a_steep_ramp_slides_downhill()
    {
        AddFloor();
        AddRamp(30f);
        _settings.GroundFriction = 0f;
        var capsule = AddCapsule(new Vector3(0f, 4f, 0f));

        Run(64);
        float x1 = PositionOf(capsule).X;
        Run(64);

        Assert.True(PositionOf(capsule).X < x1 - 0.1f);
    }

    [Fact]
    public void A_ray_hits_a_tilted_solid_on_its_face_and_reports_the_tilted_normal()
    {
        AddRamp(30f);
        Run(1);
        var ray = new Ray(new Vector3(0f, 10f, 0f), -Vector3.UnitY);

        Assert.True(_physics.Raycast(ray, 50f, out var hit, out var entity));

        Assert.False(entity.IsNull);
        var expectedNormal = new Vector3(-MathF.Sin(30f * MathF.PI / 180f), MathF.Cos(30f * MathF.PI / 180f), 0f);
        Assert.Equal(expectedNormal.X, hit.Normal.X, 3);
        Assert.Equal(expectedNormal.Y, hit.Normal.Y, 3);
        Assert.True(hit.Point.Y < 10f);
    }

    [Fact]
    public void A_ray_that_passes_beside_the_tilted_solids_corner_misses_it_though_its_bounds_would_catch_it()
    {
        AddRamp(45f); // its bounding box is much larger than the slab itself
        Run(1);
        // Inside the bounds but in the empty upper-left corner, looking up and away from the slab (which is below here).
        var ray = new Ray(new Vector3(-3.2f, 4f, 0f), Vector3.UnitY);

        Assert.False(_physics.Raycast(ray, 50f, out _, out _));
    }

    [Fact]
    public void An_oriented_collider_on_a_moving_body_is_refused()
    {
        var crate = AddCrate(new Vector3(0, 1, 0));
        _world.Get<BoxCollider3D>(crate).Oriented = true;

        Assert.Throws<InvalidOperationException>(() => Run(1));
    }

    [Fact]
    public void Axis_aligned_solids_keep_being_swept_exactly_beside_oriented_ones()
    {
        AddFloor();
        AddRamp(0f);
        var crate = AddCrate(new Vector3(30f, 0.5f, 0f)); // far from the ramp, on the plain floor

        Run(128);

        Assert.Equal(0.5f, PositionOf(crate).Y, 3);
    }
}
