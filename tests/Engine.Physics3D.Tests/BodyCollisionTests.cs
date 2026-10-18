using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics3D;

namespace Engine.Physics3D.Tests;

public class BodyCollisionTests
{
    private const double Step = 1.0 / 64.0; // a power of two keeps the arithmetic exact

    private readonly World _world = new();
    private readonly PhysicsSettings3D _settings = new();
    private readonly PhysicsSystem3D _physics;

    public BodyCollisionTests() => _physics = new PhysicsSystem3D(_settings);

    private void AddFloor()
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(0f, -0.5f, 0f));
        _world.Add(e, BoxCollider3D.Create(new Vector3(100f, 1f, 100f)));
        _world.Add(e, new StaticSolid3D());
    }

    private Entity AddBody(Vector3 position, Vector3? velocity = null, float mass = 1f, float gravityScale = 1f)
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(position));
        _world.Add(e, BoxCollider3D.Create(Vector3.One));
        _world.Add(e, new PhysicsBody3D { Velocity = velocity ?? Vector3.Zero, GravityScale = gravityScale, Mass = mass });
        return e;
    }

    private void Run(int steps)
    {
        for (int i = 0; i < steps; i++) _physics.Update(_world, Step);
    }

    private Vector3 PositionOf(Entity e) => _world.Get<Transform>(e).Position;

    private PhysicsBody3D BodyOf(Entity e) => _world.Get<PhysicsBody3D>(e);

    [Fact]
    public void A_body_dropped_on_another_comes_to_rest_on_top_of_it()
    {
        AddFloor();
        var lower = AddBody(new Vector3(0f, 0.5f, 0f));
        var upper = AddBody(new Vector3(0f, 3f, 0f));

        Run(256);

        Assert.Equal(0.5f, PositionOf(lower).Y, 3);
        Assert.Equal(1.5f, PositionOf(upper).Y, 3); // feet on the lower crate's top face at y = 1
        Assert.True(BodyOf(upper).IsGrounded);
        Assert.True(BodyOf(lower).IsGrounded);
    }

    [Fact]
    public void A_stack_of_four_stays_put()
    {
        AddFloor();
        var crates = new[]
        {
            AddBody(new Vector3(0f, 0.5f, 0f)),
            AddBody(new Vector3(0f, 1.5f, 0f)),
            AddBody(new Vector3(0f, 2.5f, 0f)),
            AddBody(new Vector3(0f, 3.5f, 0f)),
        };

        Run(512);

        for (int i = 0; i < crates.Length; i++) Assert.Equal(0.5f + i, PositionOf(crates[i]).Y, 2);
    }

    [Fact]
    public void Stepping_order_does_not_depend_on_creation_order()
    {
        AddFloor();
        var upper = AddBody(new Vector3(0f, 1.5f, 0f)); // created first although it is the higher one
        var lower = AddBody(new Vector3(0f, 0.5f, 0f));

        Run(128);

        Assert.Equal(0.5f, PositionOf(lower).Y, 3);
        Assert.Equal(1.5f, PositionOf(upper).Y, 3);
    }

    [Fact]
    public void A_moving_body_pushes_a_resting_one_and_both_end_up_moving_together()
    {
        AddFloor();
        _settings.GroundFriction = 0f;
        var pusher = AddBody(new Vector3(0f, 0.5f, 0f), velocity: new Vector3(4f, 0f, 0f));
        var pushed = AddBody(new Vector3(1.5f, 0.5f, 0f)); // a gap of 0.5 in front

        Run(64);

        // Equal masses share the momentum: each leaves at half the pusher's speed.
        Assert.Equal(2f, BodyOf(pushed).Velocity.X, 3);
        Assert.Equal(2f, BodyOf(pusher).Velocity.X, 3);
        Assert.True(PositionOf(pushed).X > 1.5f);
        Assert.True(PositionOf(pusher).X < PositionOf(pushed).X - 0.99f); // never overlapping
    }

    [Fact]
    public void A_heavy_body_barely_slows_when_it_pushes_a_light_one()
    {
        AddFloor();
        _settings.GroundFriction = 0f;
        var heavy = AddBody(new Vector3(0f, 0.5f, 0f), velocity: new Vector3(4f, 0f, 0f), mass: 9f);
        var light = AddBody(new Vector3(1.5f, 0.5f, 0f), mass: 1f);

        Run(64);

        Assert.Equal(3.6f, BodyOf(heavy).Velocity.X, 3); // (9 * 4) / 10
        Assert.Equal(3.6f, BodyOf(light).Velocity.X, 3);
    }

    [Fact]
    public void A_light_body_hitting_a_heavy_one_is_nearly_stopped()
    {
        AddFloor();
        _settings.GroundFriction = 0f;
        var light = AddBody(new Vector3(0f, 0.5f, 0f), velocity: new Vector3(5f, 0f, 0f), mass: 1f);
        AddBody(new Vector3(1.5f, 0.5f, 0f), mass: 49f);

        Run(64);

        Assert.Equal(0.1f, BodyOf(light).Velocity.X, 3); // 5 / 50
    }

    [Fact]
    public void A_body_pushed_against_a_wall_stops_instead_of_going_through_it()
    {
        AddFloor();
        _settings.GroundFriction = 0f;
        var wall = _world.CreateEntity();
        _world.Add(wall, Transform.At(4f, 5f, 0f));
        _world.Add(wall, BoxCollider3D.Create(new Vector3(1f, 10f, 100f)));
        _world.Add(wall, new StaticSolid3D());
        var pusher = AddBody(new Vector3(0f, 0.5f, 0f), velocity: new Vector3(6f, 0f, 0f));
        var pushed = AddBody(new Vector3(1.5f, 0.5f, 0f));

        Run(512);

        Assert.True(PositionOf(pushed).X <= 3f + 0.01f); // the wall's near face is at x = 3.5, the crate's half width is 0.5
        Assert.True(PositionOf(pusher).X <= PositionOf(pushed).X - 0.99f);
        Assert.Equal(0f, BodyOf(pushed).Velocity.X, 3);
    }

    [Fact]
    public void A_fast_body_cannot_tunnel_through_another_body()
    {
        AddFloor();
        _settings.GroundFriction = 0f;
        var bullet = AddBody(new Vector3(0f, 0.5f, 0f), velocity: new Vector3(200f, 0f, 0f), gravityScale: 0f, mass: 1f);
        var target = AddBody(new Vector3(3f, 0.5f, 0f), gravityScale: 0f, mass: 1000f);

        Run(8);

        Assert.True(PositionOf(bullet).X < PositionOf(target).X);
    }

    [Fact]
    public void Bodies_side_by_side_do_not_interact()
    {
        AddFloor();
        _settings.GroundFriction = 0f;
        var a = AddBody(new Vector3(0f, 0.5f, 0f), velocity: new Vector3(0f, 0f, 3f));
        AddBody(new Vector3(2f, 0.5f, 0f)); // a unit to the side of a's path, with a gap

        Run(32);

        Assert.Equal(3f, BodyOf(a).Velocity.Z, 3); // not slowed by anything
        Assert.True(PositionOf(a).Z > 0.5f);
    }

    [Fact]
    public void A_body_resting_on_another_reports_ground_contact_and_the_one_below_reports_the_weight_above_it()
    {
        AddFloor();
        var lower = AddBody(new Vector3(0f, 0.5f, 0f));
        var upper = AddBody(new Vector3(0f, 1.5f, 0f));

        Run(64);

        Assert.True((BodyOf(lower).Contacts & CollisionFlags3D.Above) != 0); // the upper crate presses on it
        Assert.True((BodyOf(upper).Contacts & CollisionFlags3D.Below) != 0);
    }

    [Fact]
    public void A_ray_can_hit_a_moving_body_and_names_its_entity()
    {
        AddFloor();
        var crate = AddBody(new Vector3(0f, 0.5f, 0f));
        Run(2);
        var ray = new Ray(new Vector3(5f, 0.5f, 0f), -Vector3.UnitX);

        Assert.True(_physics.Raycast(ray, 50f, out var hit, out var entity));

        Assert.Equal(crate, entity);
        Assert.Equal(4.5f, hit.Distance, 3);
        Assert.Equal(Vector3.UnitX, hit.Normal);
    }

    [Fact]
    public void A_body_with_no_mass_set_weighs_one()
    {
        var body = new PhysicsBody3D();

        Assert.Equal(1f, body.EffectiveMass);
        Assert.Equal(2.5f, new PhysicsBody3D { Mass = 2.5f }.EffectiveMass);
        Assert.Equal(1f, PhysicsBody3D.Create().Mass);
    }
}
