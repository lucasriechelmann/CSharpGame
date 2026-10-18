using Engine.Core.Ecs;
using Engine.Physics2D;
using Engine.Rendering;

namespace Platformer;

/// <summary>Mirrors sprites horizontally so they face the way they move. The art faces right, so moving left flips it; standing still keeps the last facing.</summary>
public sealed class FaceVelocitySystem : ISystem
{
    private const float Deadzone = 1f;

    /// <inheritdoc />
    public void Update(World world, double dt) =>
        world.Query<PhysicsBody, Sprite>((Entity _, ref PhysicsBody body, ref Sprite sprite) =>
        {
            if (body.Velocity.X < -Deadzone) sprite.Flip |= SpriteFlip.Horizontal;
            else if (body.Velocity.X > Deadzone) sprite.Flip &= ~SpriteFlip.Horizontal;
        });
}
