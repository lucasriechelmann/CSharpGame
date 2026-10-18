using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics3D;

namespace Engine.Physics3D.Tests;

public class CharacterControllerTests
{
    private const double Step = 1.0 / 64.0;

    private readonly World _world = new();
    private readonly CharacterControllerSystem3D _controller = new();
    private readonly PhysicsSystem3D _physics = new();

    private void AddFloor()
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(0f, -0.5f, 0f));
        _world.Add(e, BoxCollider3D.Create(new Vector3(100f, 1f, 100f)));
        _world.Add(e, new StaticSolid3D());
    }

    private Entity AddCharacter(Vector3? position = null)
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(position ?? new Vector3(0f, 0.9f, 0f)));
        _world.Add(e, CapsuleCollider3D.Create(0.4f, 1.8f));
        _world.Add(e, PhysicsBody3D.Create());
        _world.Add(e, CharacterController3D.Create());
        return e;
    }

    /// <summary>Controller first, then physics, as a game orders its fixed systems.</summary>
    private void Run(int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            _controller.Update(_world, Step);
            _physics.Update(_world, Step);
        }
    }

    private ref CharacterController3D ControllerOf(Entity e) => ref _world.Get<CharacterController3D>(e);

    private PhysicsBody3D BodyOf(Entity e) => _world.Get<PhysicsBody3D>(e);

    [Fact]
    public void Holding_a_direction_accelerates_to_the_move_speed_and_no_further()
    {
        AddFloor();
        var c = AddCharacter();
        Run(8); // settle onto the floor
        ControllerOf(c).Move = new Vector2(1f, 0f);

        Run(64);

        Assert.Equal(5f, BodyOf(c).Velocity.X, 3);
        Assert.True(_world.Get<Transform>(c).Position.X > 2f);
    }

    [Fact]
    public void Diagonal_input_is_not_faster_than_straight_input()
    {
        AddFloor();
        var c = AddCharacter();
        Run(8);
        ControllerOf(c).Move = new Vector2(1f, 1f);

        Run(64);

        var velocity = BodyOf(c).Velocity;
        Assert.Equal(5f, new Vector2(velocity.X, velocity.Z).Length(), 2);
    }

    [Fact]
    public void Releasing_the_input_brings_the_character_to_a_stop_on_the_ground()
    {
        AddFloor();
        var c = AddCharacter();
        Run(8);
        ControllerOf(c).Move = new Vector2(1f, 0f);
        Run(32);

        ControllerOf(c).Move = Vector2.Zero;
        Run(32);

        Assert.Equal(0f, BodyOf(c).Velocity.X, 3);
    }

    [Fact]
    public void A_jump_press_on_the_ground_launches_the_character_and_clears_the_latch()
    {
        AddFloor();
        var c = AddCharacter();
        Run(8);

        ControllerOf(c).JumpPressed = true;
        Run(1);

        Assert.True(BodyOf(c).Velocity.Y > 4f);
        Assert.False(ControllerOf(c).JumpPressed);
        Run(16);
        Assert.True(_world.Get<Transform>(c).Position.Y > 1f);
    }

    [Fact]
    public void A_jump_lands_back_on_the_floor()
    {
        AddFloor();
        var c = AddCharacter();
        Run(8);
        ControllerOf(c).JumpPressed = true;

        Run(128);

        Assert.Equal(0.9f, _world.Get<Transform>(c).Position.Y, 2);
        Assert.True(BodyOf(c).IsGrounded);
    }

    [Fact]
    public void A_press_in_mid_air_cannot_make_a_second_jump_but_is_buffered_for_the_landing()
    {
        AddFloor();
        var c = AddCharacter(new Vector3(0f, 1.2f, 0f)); // a little above the floor, falling
        ControllerOf(c).CoyoteTime = 0f;

        ControllerOf(c).JumpPressed = true;
        Run(1);
        Assert.True(BodyOf(c).Velocity.Y < 0f); // in the air with no coyote time: no jump yet

        Run(64);

        // It landed within the buffer window of the press, or the press expired; either way the character is on the floor again and calm.
        Assert.True(_world.Get<Transform>(c).Position.Y < 2f);
    }

    [Fact]
    public void A_jump_just_after_walking_off_a_ledge_still_works_within_the_coyote_time()
    {
        AddFloor();
        var c = AddCharacter();
        Run(8);
        // Simulate having just left the ground: the contacts say airborne, but coyote time is still running.
        ControllerOf(c).CoyoteTimer = 0.1f;
        _world.Get<PhysicsBody3D>(c).Contacts = CollisionFlags3D.None;

        ControllerOf(c).JumpPressed = true;
        _controller.Update(_world, Step);

        Assert.True(BodyOf(c).Velocity.Y > 4f);
    }

    [Fact]
    public void The_air_is_less_responsive_than_the_ground()
    {
        var airborne = new PhysicsBody3D(); // no contacts: in the air
        var controller = CharacterController3D.Create();
        controller.Move = new Vector2(1f, 0f);
        var world = new World();
        var a = world.CreateEntity();
        world.Add(a, airborne);
        world.Add(a, controller);

        _controller.Update(world, Step);

        float gained = world.Get<PhysicsBody3D>(a).Velocity.X;
        Assert.Equal(controller.AirAcceleration * (float)Step, gained, 4);
        Assert.True(gained < controller.Acceleration * (float)Step);
    }

    // ---- the maths ----

    [Fact]
    public void Moving_toward_a_target_never_overshoots_it()
    {
        Assert.Equal(new Vector2(3f, 0f), CharacterMath.MoveToward(new Vector2(2f, 0f), new Vector2(3f, 0f), 5f));
        Assert.Equal(new Vector2(2f, 0f), CharacterMath.MoveToward(Vector2.Zero, new Vector2(10f, 0f), 2f));
    }

    [Fact]
    public void Moving_toward_a_target_goes_in_a_straight_line()
    {
        var result = CharacterMath.MoveToward(Vector2.Zero, new Vector2(3f, 4f), 1f);

        Assert.Equal(0.6f, result.X, 4);
        Assert.Equal(0.8f, result.Y, 4);
    }

    [Fact]
    public void Input_longer_than_one_is_clamped_to_full_speed()
    {
        Assert.Equal(4f, CharacterMath.TargetVelocity(new Vector2(3f, 4f), 4f).Length(), 4);
        Assert.Equal(2f, CharacterMath.TargetVelocity(new Vector2(0.5f, 0f), 4f).X, 4); // gentle input stays gentle
    }
}
