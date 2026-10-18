using Engine.Animation;
using Engine.Core.Ecs;
using Engine.Physics2D;

namespace Platformer;

/// <summary>
/// Copies the hero's physics state (speed, vertical velocity, grounded) into its animator parameters. The animation
/// state machine then chooses idle, run, jump or fall on its own. Run it in the frame group before <see cref="AnimationSystem"/>.
/// </summary>
public sealed class HeroAnimationSystem : ISystem
{
    private readonly int _speed;
    private readonly int _velocityY;
    private readonly int _grounded;
    private readonly EntityAction<SpriteAnimator, PhysicsBody, PlatformerIntent> _apply;

    /// <summary>Creates the system, caching the parameter indices of the hero's machine.</summary>
    /// <param name="machine">The machine built by <see cref="HeroAnimations.Create"/>.</param>
    public HeroAnimationSystem(AnimationStateMachine machine)
    {
        _speed = machine.GetParameterIndex(HeroAnimations.Speed);
        _velocityY = machine.GetParameterIndex(HeroAnimations.VelocityY);
        _grounded = machine.GetParameterIndex(HeroAnimations.Grounded);
        _apply = Apply; // Created once so the per-frame query does not allocate a delegate.
    }

    /// <inheritdoc />
    public void Update(World world, double dt) => world.Query(_apply);

    // The PlatformerIntent component only exists on the hero, which is how this query singles it out.
    private void Apply(Entity entity, ref SpriteAnimator animator, ref PhysicsBody body, ref PlatformerIntent intent)
    {
        animator.SetParameter(_speed, MathF.Abs(body.Velocity.X));
        animator.SetParameter(_velocityY, body.Velocity.Y);
        animator.SetParameter(_grounded, body.IsGrounded);
    }
}
