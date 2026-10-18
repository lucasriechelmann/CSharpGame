using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics2D;

namespace Engine.Physics2D.Tests;

/// <summary>Tests platform movement and rider carrying, alone and together with the physics system, at a power-of-two step.</summary>
public class MovingPlatformSystemTests
{
    private const float Dt = 1f / 64f;

    private readonly World _world = new();
    private readonly MovingPlatformSystem _platforms = new();
    private readonly PhysicsSystem _physics = new(null);

    /// <summary>A 96x32 platform whose top is at y = 100 (centre y = 84) at the given x.</summary>
    private Entity AddPlatform(float x, Vector2 travel, float speed)
    {
        var entity = _world.CreateEntity();
        _world.Add(entity, Transform.At(x, 84f));
        _world.Add(entity, BoxCollider.Create(new Vector2(96f, 32f)));
        _world.Add(entity, new StaticSolid());
        _world.Add(entity, new MovingPlatform { Travel = travel, Speed = speed });
        return entity;
    }

    /// <summary>A 16x24 body whose feet are at <paramref name="feetY"/>.</summary>
    private Entity AddBody(float x, float feetY)
    {
        var entity = _world.CreateEntity();
        _world.Add(entity, Transform.At(x, feetY + 12f));
        _world.Add(entity, BoxCollider.Create(new Vector2(16f, 24f)));
        _world.Add(entity, PhysicsBody.Create());
        return entity;
    }

    /// <summary>One fixed step in the order the game uses: platforms first, then physics.</summary>
    private void Step(int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            _platforms.Update(_world, Dt);
            _physics.Update(_world, Dt);
        }
    }

    /// <summary>One physics step with the platforms still, so bodies know what they stand on before the platforms start to move.</summary>
    private void Settle() => _physics.Update(_world, Dt);

    private Vector3 PositionOf(Entity entity) => _world.Get<Transform>(entity).Position;

    [Fact]
    public void A_platform_moves_along_its_path_at_its_speed()
    {
        var platform = AddPlatform(0f, new Vector2(100f, 0f), speed: 64f);

        Step(32); // half a second

        Assert.Equal(32f, PositionOf(platform).X, 3);
        Assert.Equal(84f, PositionOf(platform).Y, 3);
    }

    [Fact]
    public void A_platform_follows_a_diagonal_path()
    {
        var platform = AddPlatform(0f, new Vector2(30f, 40f), speed: 50f); // path length 50

        Step(64); // one second: the whole path

        Assert.Equal(30f, PositionOf(platform).X, 2);
        Assert.Equal(124f, PositionOf(platform).Y, 2);
    }

    [Fact]
    public void A_platform_turns_around_and_returns_to_its_start()
    {
        var platform = AddPlatform(0f, new Vector2(64f, 0f), speed: 64f);

        Step(128); // out in one second, back in the next

        Assert.Equal(0f, PositionOf(platform).X, 2);
    }

    [Fact]
    public void A_platform_with_no_travel_or_speed_stays_put()
    {
        var still = AddPlatform(0f, Vector2.Zero, speed: 64f);
        var slow = AddPlatform(500f, new Vector2(100f, 0f), speed: 0f);

        Step(10);

        Assert.Equal(0f, PositionOf(still).X);
        Assert.Equal(500f, PositionOf(slow).X);
    }

    [Fact]
    public void A_rising_platform_lifts_a_body_that_was_resting_on_it_but_is_not_grounded_yet()
    {
        var platform = AddPlatform(0f, new Vector2(0f, 1000f), speed: 64f);
        var landing = AddBody(0f, 100f); // feet exactly at the top, no physics step yet: contacts unknown

        Step();

        Assert.True(_world.Get<PhysicsBody>(landing).IsGrounded);
        Assert.Equal(PositionOf(platform).Y + 16f + 12f, PositionOf(landing).Y, 2); // standing on the new top, not inside it
    }

    [Fact]
    public void A_body_far_below_a_rising_platform_is_not_lifted()
    {
        AddPlatform(0f, new Vector2(0f, 1000f), speed: 64f);
        var below = AddBody(0f, 40f); // under the platform, feet well below its top
        var before = PositionOf(below).Y;

        _platforms.Update(_world, Dt);

        Assert.Equal(before, PositionOf(below).Y);
    }

    [Fact]
    public void A_body_standing_on_a_platform_is_carried_with_it()
    {
        AddPlatform(0f, new Vector2(1000f, 0f), speed: 64f);
        var rider = AddBody(0f, 100f);
        Settle(); // contacts are only known after a physics step

        Step(64); // one second at 64 units per second

        Assert.Equal(64f, PositionOf(rider).X, 2);
        Assert.Equal(112f, PositionOf(rider).Y, 2); // still standing on it
        Assert.True(_world.Get<PhysicsBody>(rider).IsGrounded);
    }

    [Fact]
    public void A_body_is_carried_up_by_a_rising_platform()
    {
        AddPlatform(0f, new Vector2(0f, 1000f), speed: 64f);
        var rider = AddBody(0f, 100f);
        Settle();

        Step(64);

        Assert.Equal(112f + 64f, PositionOf(rider).Y, 2);
        Assert.True(_world.Get<PhysicsBody>(rider).IsGrounded);
    }

    [Fact]
    public void A_body_stays_on_a_sinking_platform()
    {
        AddPlatform(0f, new Vector2(0f, -1000f), speed: 64f);
        var rider = AddBody(0f, 100f);
        Settle();

        Step(64);

        Assert.Equal(112f - 64f, PositionOf(rider).Y, 2);
        Assert.True(_world.Get<PhysicsBody>(rider).IsGrounded);
    }

    [Fact]
    public void A_body_beside_the_platform_is_left_behind()
    {
        AddPlatform(0f, new Vector2(1000f, 0f), speed: 64f);
        var bystander = AddBody(200f, 100f); // well clear of the platform, on nothing: falls
        var before = PositionOf(bystander).X;

        Step(10);

        Assert.Equal(before, PositionOf(bystander).X);
    }

    [Fact]
    public void A_body_in_mid_air_above_the_platform_is_not_carried()
    {
        AddPlatform(0f, new Vector2(1000f, 0f), speed: 64f);
        var jumper = AddBody(0f, 150f); // 50 above the top, falling

        _platforms.Update(_world, Dt); // platforms only: no physics step has made it grounded

        Assert.Equal(0f, PositionOf(jumper).X);
    }

    [Fact]
    public void A_body_standing_beside_the_platforms_edge_is_not_carried()
    {
        var platform = AddPlatform(0f, new Vector2(1000f, 0f), speed: 64f);
        var rider = AddBody(0f, 100f);
        Settle();
        // Standing exactly beside the platform's right edge (platform spans -48..48; body is 16 wide).
        _world.Get<Transform>(rider).Position.X = 56f;
        _world.Get<PhysicsBody>(rider).Contacts = CollisionFlags.Below;

        _platforms.Update(_world, Dt);

        Assert.Equal(56f, PositionOf(rider).X);
        Assert.True(PositionOf(platform).X > 0f);
    }

    [Fact]
    public void Riders_follow_the_platform_across_a_turn_around()
    {
        AddPlatform(0f, new Vector2(64f, 0f), speed: 64f);
        var rider = AddBody(0f, 100f);
        Settle();

        Step(128); // out and back

        Assert.Equal(0f, PositionOf(rider).X, 2);
        Assert.True(_world.Get<PhysicsBody>(rider).IsGrounded);
    }

    [Fact]
    public void A_platform_works_as_a_solid_for_bodies_that_land_on_it()
    {
        AddPlatform(0f, new Vector2(1000f, 0f), speed: 64f);
        var faller = AddBody(0f, 130f);

        Step(60); // falls 30 units onto the platform and is then carried

        Assert.True(_world.Get<PhysicsBody>(faller).IsGrounded);
        Assert.Equal(112f, PositionOf(faller).Y, 1);
        Assert.True(PositionOf(faller).X > 0f, "carried once it landed");
    }

    [Fact]
    public void Moving_platforms_with_no_bodies_in_the_world_is_harmless()
    {
        AddPlatform(0f, new Vector2(100f, 0f), speed: 64f);

        Step(5);
    }
}
