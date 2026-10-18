using Engine.Physics2D;
using Engine.Physics2D.Tests.Fixtures;

namespace Engine.Physics2D.Tests;

public class PlatformerControllerTests
{
    // ---- movement ----

    [Fact]
    public void Accelerates_towards_move_speed_on_the_ground()
    {
        var sim = new PlatformerSim();
        sim.Intent.MoveX = 1f;

        sim.Step();
        Assert.Equal(2200f / 64f, sim.Body.Velocity.X, 3);

        sim.Step(30);
        Assert.Equal(220f, sim.Body.Velocity.X, 3);
    }

    [Fact]
    public void Decelerates_to_a_stop_without_input()
    {
        var sim = new PlatformerSim();
        sim.Body.Velocity.X = 100f;

        sim.Step(3); // 2600 / 64 = 40.625 per step

        Assert.Equal(0f, sim.Body.Velocity.X, 3);
    }

    [Fact]
    public void Air_control_is_weaker_than_ground_control()
    {
        var sim = new PlatformerSim(heroY: 300f);
        sim.Intent.MoveX = 1f;

        sim.Step();

        Assert.Equal(1500f / 64f, sim.Body.Velocity.X, 3);
    }

    [Fact]
    public void Can_run_left_as_well_as_right()
    {
        var sim = new PlatformerSim();
        sim.Intent.MoveX = -1f;

        sim.Step(40);

        Assert.Equal(-220f, sim.Body.Velocity.X, 3);
    }

    // ---- jumping ----

    [Fact]
    public void Pressing_jump_on_the_ground_launches_upwards()
    {
        var sim = new PlatformerSim();
        Assert.True(sim.Body.IsGrounded);

        sim.PressJump();
        sim.Step();

        // Jump speed 640 minus one step of gravity (1800 / 64).
        Assert.Equal(640f - 1800f / 64f, sim.Body.Velocity.Y, 3);
        Assert.True(sim.Transform.Position.Y > PlatformerSim.GroundY);
        Assert.True(sim.Controller.IsJumping);
        Assert.False(sim.Body.IsGrounded);
    }

    [Fact]
    public void The_pending_press_is_consumed_exactly_once()
    {
        var sim = new PlatformerSim();
        sim.PressJump();

        sim.Step();

        Assert.False(sim.Intent.JumpPressedPending);
    }

    [Fact]
    public void Holding_jump_after_landing_does_not_jump_again_without_a_new_press()
    {
        var sim = new PlatformerSim();
        sim.PressJump();
        sim.Step(200); // jump, fall, land, still holding the button

        Assert.True(sim.Body.IsGrounded);
        Assert.Equal(PlatformerSim.GroundY, sim.Transform.Position.Y, 3);
    }

    [Fact]
    public void Cannot_jump_in_mid_air_without_coyote_time()
    {
        var sim = new PlatformerSim(heroY: 300f);

        sim.PressJump();
        sim.Step();

        Assert.True(sim.Body.Velocity.Y < 0f);
        Assert.False(sim.Controller.IsJumping);
    }

    [Fact]
    public void Cannot_double_jump()
    {
        var sim = new PlatformerSim();
        sim.PressJump();
        sim.Step(5);
        float velocityBefore = sim.Body.Velocity.Y;

        sim.PressJump(); // second press in mid-air
        sim.Step();

        Assert.True(sim.Body.Velocity.Y < velocityBefore); // still just decelerating under gravity
    }

    // ---- coyote time ----

    [Fact]
    public void Coyote_time_allows_a_jump_just_after_leaving_the_ground()
    {
        var sim = new PlatformerSim();
        sim.Transform.Position.Y += 100f; // lift off the ground without a jump
        sim.Step(3);                      // 3/64 s in the air, within the 0.125 s window
        Assert.False(sim.Body.IsGrounded);

        sim.PressJump();
        sim.Step();

        Assert.True(sim.Body.Velocity.Y > 0f);
        Assert.True(sim.Controller.IsJumping);
    }

    [Fact]
    public void Coyote_time_expires()
    {
        var sim = new PlatformerSim();
        sim.Transform.Position.Y += 200f;
        sim.Step(10); // 10/64 s = 0.156 s, past the 0.125 s window, still falling

        sim.PressJump();
        sim.Step();

        Assert.True(sim.Body.Velocity.Y < 0f);
        Assert.False(sim.Controller.IsJumping);
    }

    // ---- jump buffering ----

    [Fact]
    public void A_jump_pressed_just_before_landing_fires_on_landing()
    {
        var sim = new PlatformerSim(heroY: PlatformerSim.GroundY + 12f); // 12 units above the floor, about 6 steps to land
        sim.PressJump();

        bool jumped = false;
        for (int i = 0; i < 30 && !jumped; i++)
        {
            sim.Step();
            jumped = sim.Body.Velocity.Y > 0f;
        }

        Assert.True(jumped);
    }

    [Fact]
    public void A_jump_pressed_too_early_is_forgotten()
    {
        var sim = new PlatformerSim(heroY: PlatformerSim.GroundY + 100f); // lands after the 0.125 s buffer has run out
        sim.PressJump();
        sim.Intent.JumpHeld = false; // released immediately; irrelevant to buffering but keeps the test about the buffer

        for (int i = 0; i < 60; i++)
        {
            sim.Step();
            Assert.True(sim.Body.Velocity.Y <= 0f, $"jumped unexpectedly on step {i}");
        }

        Assert.True(sim.Body.IsGrounded);
        Assert.Equal(PlatformerSim.GroundY, sim.Transform.Position.Y, 3);
    }

    // ---- variable height and gravity ----

    [Fact]
    public void Releasing_jump_early_makes_a_shorter_jump()
    {
        float held = ApexOfJump(releaseAfterSteps: null);
        float cut = ApexOfJump(releaseAfterSteps: 3);

        Assert.True(held > cut + 10f, $"held apex {held} should clearly exceed cut apex {cut}");
    }

    [Fact]
    public void Gravity_is_heavier_while_falling()
    {
        var sim = new PlatformerSim(heroY: 300f);

        sim.Step(5);

        Assert.Equal(sim.Controller.Tuning.FallGravityMultiplier, sim.Body.GravityScale);
    }

    [Fact]
    public void Gravity_is_normal_while_rising()
    {
        var sim = new PlatformerSim();
        sim.PressJump();

        sim.Step();

        Assert.Equal(1f, sim.Body.GravityScale);
    }

    private static float ApexOfJump(int? releaseAfterSteps)
    {
        var sim = new PlatformerSim();
        sim.PressJump();

        float apex = sim.Transform.Position.Y;
        for (int i = 0; i < 90; i++)
        {
            if (releaseAfterSteps is { } release && i == release) sim.Intent.JumpHeld = false;
            sim.Step();
            apex = MathF.Max(apex, sim.Transform.Position.Y);
        }

        return apex;
    }
}
