using Engine.Core.Ecs;

namespace Platformer;

/// <summary>Moves every entity that has both a <see cref="Position"/> and a <see cref="Velocity"/>.</summary>
public sealed class MovementSystem : ISystem
{
    /// <inheritdoc />
    public void Update(World world, double dt) =>
        world.Query<Position, Velocity>((Entity _, ref Position p, ref Velocity v) =>
            p.Value += v.Value * (float)dt);
}
