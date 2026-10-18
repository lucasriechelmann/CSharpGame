using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics3D;

namespace Engine.Physics3D.Tests;

public class PhysicsSystem3DTests
{
    private const double Step = 1.0 / 64.0; // a power of two keeps the arithmetic exact

    private readonly World _world = new();
    private readonly PhysicsSettings3D _settings = new();
    private readonly PhysicsSystem3D _physics;

    public PhysicsSystem3DTests() => _physics = new PhysicsSystem3D(_settings);

    private Entity AddFloor()
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(0f, -0.5f, 0f)); // a 1-unit slab whose top face is y = 0
        _world.Add(e, BoxCollider3D.Create(new Vector3(100f, 1f, 100f)));
        _world.Add(e, new StaticSolid3D());
        return e;
    }

    private Entity AddWall(float x)
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(x, 5f, 0f));
        _world.Add(e, BoxCollider3D.Create(new Vector3(1f, 10f, 100f)));
        _world.Add(e, new StaticSolid3D());
        return e;
    }

    private Entity AddBody(Vector3 position, Vector3? velocity = null, float gravityScale = 1f)
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(position));
        _world.Add(e, BoxCollider3D.Create(Vector3.One)); // 1x1x1, so a body at height h has its feet at h - 0.5
        _world.Add(e, new PhysicsBody3D { Velocity = velocity ?? Vector3.Zero, GravityScale = gravityScale });
        return e;
    }

    private void Run(int steps)
    {
        for (int i = 0; i < steps; i++) _physics.Update(_world, Step);
    }

    [Fact]
    public void A_body_in_the_air_falls_under_gravity()
    {
        var body = AddBody(new Vector3(0f, 10f, 0f));

        Run(32);

        Assert.True(_world.Get<Transform>(body).Position.Y < 10f);
        Assert.True(_world.Get<PhysicsBody3D>(body).Velocity.Y < 0f);
    }

    [Fact]
    public void A_falling_body_lands_on_the_floor_and_rests_there_grounded()
    {
        AddFloor();
        var body = AddBody(new Vector3(0f, 3f, 0f));

        Run(256);

        Assert.Equal(0.5f, _world.Get<Transform>(body).Position.Y, 3); // feet on the floor top at y = 0
        var physics = _world.Get<PhysicsBody3D>(body);
        Assert.True(physics.IsGrounded);
        Assert.Equal(0f, physics.Velocity.Y);
    }

    [Fact]
    public void Friction_stops_a_sliding_body_without_reversing_it()
    {
        AddFloor();
        var body = AddBody(new Vector3(0f, 0.5f, 0f), velocity: new Vector3(6f, 0f, 0f));

        Run(512); // 8 seconds is far longer than the 6 / 12 = 0.5 s it takes to stop

        var position = _world.Get<Transform>(body).Position;
        Assert.Equal(0f, _world.Get<PhysicsBody3D>(body).Velocity.X);
        Assert.InRange(position.X, 1.4f, 1.6f); // v^2 / (2 * friction) = 36 / 24 = 1.5 metres
    }

    [Fact]
    public void A_frictionless_floor_lets_a_body_keep_sliding()
    {
        var settings = new PhysicsSettings3D { GroundFriction = 0f };
        var physics = new PhysicsSystem3D(settings);
        AddFloor();
        var body = AddBody(new Vector3(0f, 0.5f, 0f), velocity: new Vector3(6f, 0f, 0f));

        for (int i = 0; i < 128; i++) physics.Update(_world, Step);

        Assert.Equal(6f, _world.Get<PhysicsBody3D>(body).Velocity.X);
    }

    [Fact]
    public void Friction_does_not_slow_a_body_in_the_air()
    {
        var body = AddBody(new Vector3(0f, 1000f, 0f), velocity: new Vector3(6f, 0f, 0f));

        Run(64);

        Assert.Equal(6f, _world.Get<PhysicsBody3D>(body).Velocity.X);
    }

    [Fact]
    public void Terminal_velocity_limits_the_fall_speed()
    {
        var body = AddBody(new Vector3(0f, 10000f, 0f));

        Run(2000);

        Assert.Equal(-50f, _world.Get<PhysicsBody3D>(body).Velocity.Y);
    }

    [Fact]
    public void Gravity_scale_zero_floats()
    {
        var body = AddBody(new Vector3(0f, 10f, 0f), gravityScale: 0f);

        Run(64);

        Assert.Equal(10f, _world.Get<Transform>(body).Position.Y);
    }

    [Fact]
    public void A_body_moving_into_a_wall_stops_at_it_and_reports_the_contact()
    {
        _settings.GroundFriction = 0f; // the body must reach the wall, not stop on the way
        AddFloor();
        AddWall(5f); // face at x = 4.5
        var body = AddBody(new Vector3(0f, 0.5f, 0f), velocity: new Vector3(8f, 0f, 0f));

        Run(128);

        var position = _world.Get<Transform>(body).Position;
        Assert.Equal(4f, position.X, 3); // body half-width 0.5 flush with the wall face
        var physics = _world.Get<PhysicsBody3D>(body);
        Assert.True((physics.Contacts & CollisionFlags3D.MaxX) != 0);
        Assert.Equal(0f, physics.Velocity.X);
        Assert.True(physics.IsGrounded); // sliding into the wall did not lift it off the floor
    }

    [Fact]
    public void A_diagonal_move_into_a_wall_slides_along_it()
    {
        _settings.GroundFriction = 0f;
        AddFloor();
        AddWall(5f);
        var body = AddBody(new Vector3(0f, 0.5f, 0f), velocity: new Vector3(8f, 0f, 4f));

        Run(128);

        var position = _world.Get<Transform>(body).Position;
        Assert.Equal(4f, position.X, 3);
        Assert.True(position.Z > 1f, "the Z part of the motion must survive the X collision");
    }

    [Fact]
    public void A_fast_body_does_not_tunnel_through_a_thin_floor()
    {
        var floor = _world.CreateEntity();
        _world.Add(floor, Transform.At(0f, -0.005f, 0f));
        _world.Add(floor, BoxCollider3D.Create(new Vector3(100f, 0.01f, 100f)));
        _world.Add(floor, new StaticSolid3D());
        var body = AddBody(new Vector3(0f, 40f, 0f), velocity: new Vector3(0f, -3000f, 0f), gravityScale: 0f);

        Run(4);

        Assert.True(_world.Get<Transform>(body).Position.Y >= 0.5f - 0.001f);
    }

    [Fact]
    public void A_body_resting_against_a_wall_keeps_reporting_the_contact_with_no_velocity()
    {
        AddFloor();
        AddWall(5f);
        var body = AddBody(new Vector3(4f, 0.5f, 0f), gravityScale: 0f); // already touching the wall, at rest

        Run(3);

        Assert.True((_world.Get<PhysicsBody3D>(body).Contacts & CollisionFlags3D.MaxX) != 0);
    }

    [Fact]
    public void Static_solids_do_not_move()
    {
        var floor = AddFloor();

        Run(32);

        Assert.Equal(-0.5f, _world.Get<Transform>(floor).Position.Y);
    }

    [Fact]
    public void Raycast_through_the_system_returns_the_entity_that_was_hit()
    {
        var floor = AddFloor();
        var wall = AddWall(5f);
        Run(1); // gathers the solids

        Assert.True(_physics.Raycast(new Ray(new Vector3(0f, 2f, 0f), Vector3.UnitX), 50f, out var hit, out var entity));
        Assert.Equal(wall, entity);
        Assert.Equal(4.5f, hit.Distance, 4);

        Assert.True(_physics.Raycast(new Ray(new Vector3(0f, 2f, 0f), -Vector3.UnitY), 50f, out _, out var below));
        Assert.Equal(floor, below);
    }

    [Fact]
    public void Raycast_that_misses_returns_the_null_entity()
    {
        AddFloor();
        Run(1);

        Assert.False(_physics.Raycast(new Ray(new Vector3(0f, 2f, 0f), Vector3.UnitY), 50f, out _, out var entity));
        Assert.Equal(Entity.Null, entity);
    }
}
