using Engine.Core;
using Engine.Core.Ecs;

namespace Engine.Physics2D;

/// <summary>
/// Turns <see cref="PlatformerIntent"/> into velocity changes with acceleration, coyote time, jump buffering, variable
/// jump height and a heavier fall. Run it in the fixed-step group <b>before</b> <see cref="PhysicsSystem"/>, which
/// applies the resulting velocity and reports the contacts this system reads on the next step.
/// </summary>
public sealed class PlatformerControllerSystem : ISystem
{
    private readonly EntityAction<PlatformerController, PlatformerIntent, PhysicsBody> _step;
    private float _dt;

    /// <summary>Creates the system.</summary>
    public PlatformerControllerSystem()
    {
        _step = Step; // Created once so the per-step query does not allocate a delegate.
    }

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _dt = (float)dt;
        world.Query(_step);
    }

    private void Step(Entity entity, ref PlatformerController controller, ref PlatformerIntent intent, ref PhysicsBody body)
    {
        var tuning = controller.Tuning;
        bool grounded = body.IsGrounded;

        // A jump press arrives as a one-shot flag; remember it for a short while (the jump buffer).
        if (intent.JumpPressedPending)
        {
            controller.JumpBufferTimer = tuning.JumpBufferTime;
            intent.JumpPressedPending = false;
        }

        // Standing on the ground refills coyote time; in the air it drains.
        controller.CoyoteTimer = grounded ? tuning.CoyoteTime : MathF.Max(0f, controller.CoyoteTimer - _dt);

        // Horizontal: approach the target speed, faster when pressing a direction than when letting go.
        float target = intent.MoveX * tuning.MoveSpeed;
        float acceleration = target == 0f
            ? (grounded ? tuning.GroundDeceleration : tuning.AirDeceleration)
            : (grounded ? tuning.GroundAcceleration : tuning.AirAcceleration);
        body.Velocity.X = MathHelper.MoveToward(body.Velocity.X, target, acceleration * _dt);

        // Jump when a press is remembered and the ground (or recent ground) is under us.
        if (controller.JumpBufferTimer > 0f && controller.CoyoteTimer > 0f)
        {
            body.Velocity.Y = tuning.JumpSpeed;
            controller.JumpBufferTimer = 0f;
            controller.CoyoteTimer = 0f; // consumed, so there is no double jump
            controller.IsJumping = true;
        }

        // Variable height: letting go while still rising cuts the jump short, once.
        if (controller.IsJumping)
        {
            if (body.Velocity.Y <= 0f)
            {
                controller.IsJumping = false;
            }
            else if (!intent.JumpHeld)
            {
                body.Velocity.Y *= tuning.JumpCutMultiplier;
                controller.IsJumping = false;
            }
        }

        controller.JumpBufferTimer = MathF.Max(0f, controller.JumpBufferTimer - _dt);

        // Fall faster than we rise for a snappier arc.
        body.GravityScale = body.Velocity.Y < 0f ? tuning.FallGravityMultiplier : 1f;
    }
}
