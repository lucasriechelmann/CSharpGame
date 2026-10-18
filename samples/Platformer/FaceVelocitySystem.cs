using Engine.Core.Ecs;
using Engine.Rendering;

namespace Platformer;

/// <summary>Mirrors sprites horizontally so they face the way they move. The art faces right, so moving left flips it.</summary>
public sealed class FaceVelocitySystem : ISystem
{
    private const float Deadzone = 1f;

    /// <inheritdoc />
    public void Update(World world, double dt) =>
        world.Query<Velocity, Sprite>((Entity _, ref Velocity velocity, ref Sprite sprite) =>
        {
            if (velocity.Value.X < -Deadzone) sprite.Flip |= SpriteFlip.Horizontal;
            else if (velocity.Value.X > Deadzone) sprite.Flip &= ~SpriteFlip.Horizontal;
        });
}
