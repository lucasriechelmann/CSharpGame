using Engine.Core.Components;
using Engine.Core.Ecs;

namespace Platformer;

/// <summary>Reflects <see cref="Bouncer"/> entities off a rectangular arena centred on the origin so the demo creatures stay on screen.</summary>
/// <param name="halfWidth">Half the arena width in world units.</param>
/// <param name="halfHeight">Half the arena height in world units.</param>
public sealed class BoundsBounceSystem(float halfWidth, float halfHeight) : ISystem
{
    /// <inheritdoc />
    public void Update(World world, double dt) =>
        world.Query<Transform, Velocity, Bouncer>((Entity _, ref Transform t, ref Velocity v, ref Bouncer _) =>
        {
            if (t.Position.X < -halfWidth && v.Value.X < 0 || t.Position.X > halfWidth && v.Value.X > 0)
                v.Value.X = -v.Value.X;
            if (t.Position.Y < -halfHeight && v.Value.Y < 0 || t.Position.Y > halfHeight && v.Value.Y > 0)
                v.Value.Y = -v.Value.Y;
        });
}
