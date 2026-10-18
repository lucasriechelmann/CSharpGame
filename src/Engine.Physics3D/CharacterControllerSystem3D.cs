using System.Numerics;
using Engine.Core.Ecs;

namespace Engine.Physics3D;

/// <summary>
/// Turns each <see cref="CharacterController3D"/>'s intent into velocity on its <see cref="PhysicsBody3D"/>: it accelerates the horizontal speed
/// towards the wanted one and starts jumps (with coyote time and a jump buffer, like the 2D platformer controller). A fixed system: add it
/// to <c>FixedSystems</c> <b>before</b> <see cref="PhysicsSystem3D"/>, which then moves the body and updates the contacts this reads next step.
/// </summary>
public sealed class CharacterControllerSystem3D : ISystem
{
    private readonly EntityAction<PhysicsBody3D, CharacterController3D> _apply;
    private float _dt;

    /// <summary>Creates the system.</summary>
    public CharacterControllerSystem3D() => _apply = Apply; // created once so the per-step query does not allocate a delegate

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _dt = (float)dt;
        world.Query(_apply);
    }

    private void Apply(Entity entity, ref PhysicsBody3D body, ref CharacterController3D controller)
    {
        bool grounded = body.IsGrounded;

        var wanted = CharacterMath.TargetVelocity(controller.Move, controller.MoveSpeed);
        var current = new Vector2(body.Velocity.X, body.Velocity.Z);
        float acceleration = grounded ? controller.Acceleration : controller.AirAcceleration;
        current = CharacterMath.MoveToward(current, wanted, acceleration * _dt);
        body.Velocity.X = current.X;
        body.Velocity.Z = current.Y;

        // Timers: coyote time is refilled on the ground and drains in the air; a press fills the buffer, which then drains too.
        controller.CoyoteTimer = grounded ? controller.CoyoteTime : MathF.Max(controller.CoyoteTimer - _dt, 0f);
        if (controller.JumpPressed)
        {
            controller.JumpBufferTimer = controller.JumpBufferTime;
            controller.JumpPressed = false;
        }
        else
        {
            controller.JumpBufferTimer = MathF.Max(controller.JumpBufferTimer - _dt, 0f);
        }

        if (controller.JumpBufferTimer > 0f && controller.CoyoteTimer > 0f)
        {
            body.Velocity.Y = controller.JumpSpeed;
            controller.JumpBufferTimer = 0f;
            controller.CoyoteTimer = 0f;
        }
    }
}
