using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics2D;

namespace Engine.Physics2D.Tests;

/// <summary>Tests the physics system alone: a 40x20 grid of 16-unit tiles with a solid floor row (top at y = 16).</summary>
public class PhysicsSystemTests
{
    private const float Dt = 1f / 64f;

    private readonly World _world = new();
    private readonly TileGrid _grid = new(40, 20, 16f, Vector2.Zero);
    private readonly PhysicsSettings _settings = new();
    private readonly PhysicsSystem _physics;

    public PhysicsSystemTests()
    {
        for (int x = 0; x < _grid.Width; x++) _grid.SetSolid(x, 0, true);
        _physics = new PhysicsSystem(_grid, _settings);
    }

    private Entity SpawnBody(float x, float y, Vector2? size = null, Vector2? offset = null)
    {
        var entity = _world.CreateEntity();
        _world.Add(entity, Transform.At(x, y));
        _world.Add(entity, PhysicsBody.Create());
        _world.Add(entity, new BoxCollider { Size = size ?? new Vector2(16f, 24f), Offset = offset ?? Vector2.Zero });
        return entity;
    }

    private void Step(int count = 1)
    {
        for (int i = 0; i < count; i++) _physics.Update(_world, Dt);
    }

    [Fact]
    public void Replacing_the_grid_changes_what_bodies_collide_with()
    {
        var e = SpawnBody(100f, 40f);
        Step(30);
        Assert.True(_world.Get<PhysicsBody>(e).IsGrounded); // standing on the floor row of the original grid

        _physics.Grid = null; // no collision at all
        Step(30);

        Assert.False(_world.Get<PhysicsBody>(e).IsGrounded);
        Assert.True(_world.Get<Transform>(e).Position.Y < 16f, "fell through where the floor used to be");
    }

    [Fact]
    public void Gravity_accelerates_a_falling_body()
    {
        var e = SpawnBody(100f, 300f);

        Step();

        // 1800 / 64 = 28.125 per step, then moved by velocity * dt.
        Assert.Equal(-28.125f, _world.Get<PhysicsBody>(e).Velocity.Y);
        Assert.Equal(300f - 28.125f / 64f, _world.Get<Transform>(e).Position.Y, 4);
    }

    [Fact]
    public void A_falling_body_lands_on_the_floor_and_stops()
    {
        var e = SpawnBody(100f, 200f);

        Step(120);

        var body = _world.Get<PhysicsBody>(e);
        Assert.True(body.IsGrounded);
        Assert.Equal(0f, body.Velocity.Y);
        Assert.Equal(28f, _world.Get<Transform>(e).Position.Y, 3); // 16 floor top + 12 half height
    }

    [Fact]
    public void Fall_speed_is_capped_at_the_terminal_velocity()
    {
        var e = SpawnBody(100f, 100_000f);

        Step(200);

        Assert.Equal(-_settings.MaxFallSpeed, _world.Get<PhysicsBody>(e).Velocity.Y);
    }

    [Fact]
    public void A_wall_stops_horizontal_movement_and_reports_contact()
    {
        for (int y = 0; y < _grid.Height; y++) _grid.SetSolid(30, y, true); // wall face at x = 480
        var e = SpawnBody(100f, 28f);
        _world.Get<PhysicsBody>(e).Velocity.X = 1000f;

        Step(120);

        var body = _world.Get<PhysicsBody>(e);
        Assert.Equal(472f, _world.Get<Transform>(e).Position.X, 3); // 480 - half width 8
        Assert.Equal(0f, body.Velocity.X);
        Assert.True((body.Contacts & CollisionFlags.Right) != 0);
    }

    [Fact]
    public void A_wall_on_the_left_reports_left_contact()
    {
        for (int y = 0; y < _grid.Height; y++) _grid.SetSolid(5, y, true); // wall face at x = 96
        var e = SpawnBody(200f, 28f);
        _world.Get<PhysicsBody>(e).Velocity.X = -1000f;

        Step(120);

        Assert.Equal(104f, _world.Get<Transform>(e).Position.X, 3); // 96 + half width 8
        Assert.True((_world.Get<PhysicsBody>(e).Contacts & CollisionFlags.Left) != 0);
    }

    [Fact]
    public void A_ceiling_stops_upward_movement()
    {
        for (int x = 0; x < _grid.Width; x++) _grid.SetSolid(x, 10, true); // underside at y = 160
        var e = SpawnBody(100f, 100f);
        _world.Get<PhysicsBody>(e).Velocity.Y = 2000f;

        for (int i = 0; i < 10 && (_world.Get<PhysicsBody>(e).Contacts & CollisionFlags.Above) == 0; i++)
            Step(); // stop on the step the head touches, before gravity pulls it back down

        Assert.Equal(148f, _world.Get<Transform>(e).Position.Y, 3); // 160 - half height 12
        Assert.True((_world.Get<PhysicsBody>(e).Contacts & CollisionFlags.Above) != 0);
    }

    [Fact]
    public void Contacts_are_cleared_when_the_body_leaves_the_surface()
    {
        var e = SpawnBody(100f, 28f);
        Step(2);
        Assert.True(_world.Get<PhysicsBody>(e).IsGrounded);

        _world.Get<PhysicsBody>(e).Velocity.Y = 600f;
        Step();

        Assert.Equal(CollisionFlags.None, _world.Get<PhysicsBody>(e).Contacts);
    }

    [Fact]
    public void A_resting_body_keeps_reporting_ground_contact_even_with_no_gravity()
    {
        var e = SpawnBody(100f, 28f);
        _world.Get<PhysicsBody>(e).GravityScale = 0f;

        Step(5);

        Assert.True(_world.Get<PhysicsBody>(e).IsGrounded);
    }

    [Fact]
    public void Body_slides_along_the_floor_while_moving_sideways()
    {
        var e = SpawnBody(100f, 28f);
        _world.Get<PhysicsBody>(e).Velocity.X = 64f; // exactly one unit per step

        Step(10);

        Assert.Equal(110f, _world.Get<Transform>(e).Position.X, 3);
        Assert.Equal(28f, _world.Get<Transform>(e).Position.Y, 3);
        Assert.True(_world.Get<PhysicsBody>(e).IsGrounded);
    }

    [Fact]
    public void Static_solid_entities_block_bodies()
    {
        var platform = _world.CreateEntity();
        _world.Add(platform, Transform.At(100f, 100f));
        _world.Add(platform, BoxCollider.Create(new Vector2(64f, 16f))); // top at y = 108
        _world.Add(platform, new StaticSolid());
        var e = SpawnBody(100f, 200f);

        Step(120);

        Assert.Equal(120f, _world.Get<Transform>(e).Position.Y, 3); // 108 + half height 12
        Assert.True(_world.Get<PhysicsBody>(e).IsGrounded);
    }

    [Fact]
    public void Colliders_without_the_static_tag_do_not_block()
    {
        var decoration = _world.CreateEntity();
        _world.Add(decoration, Transform.At(100f, 100f));
        _world.Add(decoration, BoxCollider.Create(new Vector2(64f, 16f))); // no StaticSolid tag
        var e = SpawnBody(100f, 200f);

        Step(120);

        Assert.Equal(28f, _world.Get<Transform>(e).Position.Y, 3); // fell through to the real floor
    }

    [Fact]
    public void Physics_leaves_Z_alone()
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(100f, 200f, 7f));
        _world.Add(e, PhysicsBody.Create());
        _world.Add(e, BoxCollider.Create(new Vector2(16f, 24f)));

        Step(30);

        Assert.Equal(7f, _world.Get<Transform>(e).Position.Z);
    }

    [Fact]
    public void Collider_offset_shifts_the_box_relative_to_the_entity()
    {
        var e = SpawnBody(100f, 200f, offset: new Vector2(0f, -4f)); // box sits 4 below the entity position

        Step(120);

        Assert.Equal(32f, _world.Get<Transform>(e).Position.Y, 3); // box centre 28, so entity is 4 higher
    }

    [Fact]
    public void Gravity_scale_scales_the_fall()
    {
        var floaty = SpawnBody(100f, 300f);
        var heavy = SpawnBody(200f, 300f);
        _world.Get<PhysicsBody>(floaty).GravityScale = 0f;
        _world.Get<PhysicsBody>(heavy).GravityScale = 2f;

        Step();

        Assert.Equal(0f, _world.Get<PhysicsBody>(floaty).Velocity.Y);
        Assert.Equal(-56.25f, _world.Get<PhysicsBody>(heavy).Velocity.Y);
    }

    [Fact]
    public void Bodies_without_a_collider_are_ignored()
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(100f, 300f));
        _world.Add(e, PhysicsBody.Create());

        Step(10);

        Assert.Equal(300f, _world.Get<Transform>(e).Position.Y);
    }
}
