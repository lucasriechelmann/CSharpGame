using Engine.Core.Components;
using Engine.Core.Ecs;

namespace Platformer;

/// <summary>Moves every entity that has both a <see cref="Transform"/> and a <see cref="Velocity"/>.</summary>
public sealed class MovementSystem : ISystem
{
    /// <inheritdoc />
    public void Update(World world, double dt) =>
        world.Query<Transform, Velocity>((Entity _, ref Transform t, ref Velocity v) =>
            t.Position += v.Value * (float)dt);
}
