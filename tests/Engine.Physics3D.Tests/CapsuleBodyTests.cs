using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics3D;

namespace Engine.Physics3D.Tests;

public class CapsuleBodyTests
{
    private const double Step = 1.0 / 64.0; // a power of two keeps the arithmetic exact

    private readonly World _world = new();
    private readonly PhysicsSettings3D _settings = new();
    private readonly PhysicsSystem3D _physics;

    public CapsuleBodyTests() => _physics = new PhysicsSystem3D(_settings);

    private void AddFloor()
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(0f, -0.5f, 0f));
        _world.Add(e, BoxCollider3D.Create(new Vector3(100f, 1f, 100f)));
        _world.Add(e, new StaticSolid3D());
    }

    private void AddWall(float x)
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(x, 5f, 0f));
        _world.Add(e, BoxCollider3D.Create(new Vector3(1f, 10f, 100f)));
        _world.Add(e, new StaticSolid3D());
    }

    /// <summary>A capsule 1.8 tall and 0.4 wide in radius, standing on its centre (so at height h its feet are at h - 0.9).</summary>
    private Entity AddCapsule(Vector3 position, Vector3? velocity = null, float mass = 1f)
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(position));
        _world.Add(e, CapsuleCollider3D.Create(0.4f, 1.8f));
        _world.Add(e, new PhysicsBody3D { Velocity = velocity ?? Vector3.Zero, GravityScale = 1f, Mass = mass });
        return e;
    }

    private Entity AddSphere(Vector3 position, float radius = 0.5f)
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(position));
        _world.Add(e, CapsuleCollider3D.Sphere(radius));
        _world.Add(e, PhysicsBody3D.Create());
        return e;
    }

    private Entity AddCrate(Vector3 position, float mass = 1f)
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(position));
        _world.Add(e, BoxCollider3D.Create(Vector3.One));
        _world.Add(e, new PhysicsBody3D { GravityScale = 1f, Mass = mass });
        return e;
    }

    private void Run(int steps)
    {
        for (int i = 0; i < steps; i++) _physics.Update(_world, Step);
    }

    private Vector3 PositionOf(Entity e) => _world.Get<Transform>(e).Position;

    private PhysicsBody3D BodyOf(Entity e) => _world.Get<PhysicsBody3D>(e);

    [Fact]
    public void A_capsule_falls_lands_and_rests_grounded_with_its_feet_on_the_floor()
    {
        AddFloor();
        var capsule = AddCapsule(new Vector3(0f, 4f, 0f));

        Run(256);

        Assert.Equal(0.9f, PositionOf(capsule).Y, 2);
        Assert.True(BodyOf(capsule).IsGrounded);
        Assert.Equal(0f, BodyOf(capsule).Velocity.Y, 2);
    }

    [Fact]
    public void A_sphere_comes_to_rest_with_its_underside_on_the_floor()
    {
        AddFloor();
        var sphere = AddSphere(new Vector3(0f, 3f, 0f), radius: 0.5f);

        Run(256);

        Assert.Equal(0.5f, PositionOf(sphere).Y, 2);
        Assert.True(BodyOf(sphere).IsGrounded);
    }

    [Fact]
    public void A_capsule_resting_on_the_floor_does_not_creep_or_sink_over_time()
    {
        AddFloor();
        var capsule = AddCapsule(new Vector3(0f, 0.9f, 0f));

        Run(64);
        var settled = PositionOf(capsule);
        Run(512);

        Assert.Equal(settled.Y, PositionOf(capsule).Y, 3);
        Assert.Equal(settled.X, PositionOf(capsule).X, 4);
    }

    [Fact]
    public void A_capsule_walking_into_a_wall_stops_against_it_and_does_not_go_through()
    {
        AddFloor();
        AddWall(5f); // near face at x = 4.5
        _settings.GroundFriction = 0f;
        var capsule = AddCapsule(new Vector3(0f, 0.9f, 0f), velocity: new Vector3(8f, 0f, 0f));

        Run(256);

        Assert.True(PositionOf(capsule).X <= 4.5f - 0.4f + 0.01f);
        Assert.True(PositionOf(capsule).X > 3.5f);
        Assert.Equal(0f, BodyOf(capsule).Velocity.X, 3);
        Assert.True((BodyOf(capsule).Contacts & CollisionFlags3D.MaxX) != 0);
    }

    [Fact]
    public void A_capsule_hitting_a_wall_at_an_angle_slides_along_it()
    {
        AddFloor();
        AddWall(5f);
        _settings.GroundFriction = 0f;
        var capsule = AddCapsule(new Vector3(0f, 0.9f, 0f), velocity: new Vector3(6f, 0f, 6f));

        Run(64);

        Assert.Equal(0f, BodyOf(capsule).Velocity.X, 3); // the part into the wall is gone
        Assert.Equal(6f, BodyOf(capsule).Velocity.Z, 3); // the part along it is kept
        Assert.True(PositionOf(capsule).Z > 4f);
    }

    [Fact]
    public void A_very_fast_capsule_cannot_tunnel_through_a_thin_wall()
    {
        AddFloor();
        var wall = _world.CreateEntity();
        _world.Add(wall, Transform.At(5f, 2f, 0f));
        _world.Add(wall, BoxCollider3D.Create(new Vector3(0.1f, 4f, 20f)));
        _world.Add(wall, new StaticSolid3D());
        _settings.GroundFriction = 0f;
        var capsule = AddCapsule(new Vector3(0f, 0.9f, 0f), velocity: new Vector3(150f, 0f, 0f));

        Run(32);

        Assert.True(PositionOf(capsule).X < 5f);
    }

    [Fact]
    public void A_capsule_does_not_catch_on_the_seam_between_two_floor_slabs()
    {
        for (int i = 0; i < 2; i++)
        {
            var slab = _world.CreateEntity();
            _world.Add(slab, Transform.At(-5f + 10f * i, -0.5f, 0f)); // two 10 x 10 slabs meeting at x = 0
            _world.Add(slab, BoxCollider3D.Create(new Vector3(10f, 1f, 10f)));
            _world.Add(slab, new StaticSolid3D());
        }

        _settings.GroundFriction = 0f;
        var capsule = AddCapsule(new Vector3(-3f, 0.9f, 0f), velocity: new Vector3(4f, 0f, 0f));

        Run(128);

        Assert.True(PositionOf(capsule).X > 1f);
        Assert.Equal(0.9f, PositionOf(capsule).Y, 2);
    }

    [Fact]
    public void A_capsule_lands_on_a_crate_and_stands_on_it()
    {
        AddFloor();
        var crate = AddCrate(new Vector3(0f, 0.5f, 0f));
        var capsule = AddCapsule(new Vector3(0f, 4f, 0f));

        Run(256);

        Assert.Equal(0.5f, PositionOf(crate).Y, 2);
        Assert.Equal(1.9f, PositionOf(capsule).Y, 2); // feet on the crate's top at y = 1
        Assert.True(BodyOf(capsule).IsGrounded);
    }

    [Fact]
    public void A_walking_capsule_pushes_a_crate_along()
    {
        AddFloor();
        _settings.GroundFriction = 0f;
        var capsule = AddCapsule(new Vector3(0f, 0.9f, 0f), velocity: new Vector3(3f, 0f, 0f), mass: 80f);
        var crate = AddCrate(new Vector3(1.5f, 0.5f, 0f), mass: 20f);

        Run(64);

        Assert.True(PositionOf(crate).X > 1.5f);
        Assert.True(BodyOf(crate).Velocity.X > 2f); // a heavy body shoves a light one along at nearly its own speed
        Assert.True(PositionOf(capsule).X + 0.4f <= PositionOf(crate).X - 0.5f + 0.01f); // never inside it
    }

    [Fact]
    public void A_falling_crate_lands_on_a_standing_capsule_and_rests_on_its_head()
    {
        AddFloor();
        var capsule = AddCapsule(new Vector3(0f, 0.9f, 0f), mass: 80f);
        var crate = AddCrate(new Vector3(0f, 4f, 0f));

        Run(256);

        // The crate sees the capsule as its bounding box, so it rests on the flat top at y = 1.8.
        Assert.Equal(0.9f, PositionOf(capsule).Y, 2);
        Assert.Equal(2.3f, PositionOf(crate).Y, 2);
    }

    [Fact]
    public void A_ray_can_hit_a_capsule_body()
    {
        AddFloor();
        var capsule = AddCapsule(new Vector3(0f, 0.9f, 0f));
        Run(2);

        Assert.True(_physics.Raycast(new Ray(new Vector3(5f, 0.9f, 0f), -Vector3.UnitX), 50f, out _, out var entity));
        Assert.Equal(capsule, entity);
    }

    [Fact]
    public void An_entity_with_both_collider_kinds_is_refused()
    {
        var e = AddCapsule(Vector3.Zero);
        _world.Add(e, BoxCollider3D.Create(Vector3.One));

        Assert.Throws<InvalidOperationException>(() => Run(1));
    }

    [Fact]
    public void A_capsule_with_no_radius_is_refused()
    {
        var e = AddCapsule(Vector3.Zero);
        _world.Get<CapsuleCollider3D>(e).Radius = 0f;

        Assert.Throws<InvalidOperationException>(() => Run(1));
    }

    [Fact]
    public void Ground_friction_does_not_apply_to_a_controlled_character()
    {
        AddFloor();
        var capsule = AddCapsule(new Vector3(0f, 0.9f, 0f));
        _world.Add(capsule, CharacterController3D.Create());
        Run(8); // grounded now
        _world.Get<PhysicsBody3D>(capsule).Velocity.X = 3f;

        Run(8);

        Assert.Equal(3f, BodyOf(capsule).Velocity.X, 3); // a plain body would have lost 12 * 8/64 = 1.5 m/s
    }

    [Fact]
    public void Ground_friction_still_stops_a_plain_capsule()
    {
        AddFloor();
        var capsule = AddCapsule(new Vector3(0f, 0.9f, 0f));
        Run(8);
        _world.Get<PhysicsBody3D>(capsule).Velocity.X = 3f;

        Run(8);

        Assert.True(BodyOf(capsule).Velocity.X < 2f);
    }
}
