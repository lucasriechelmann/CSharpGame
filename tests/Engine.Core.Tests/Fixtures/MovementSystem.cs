using Engine.Core.Ecs;

namespace Engine.Core.Tests.Fixtures;

/// <summary>Test system: integrates <see cref="Velocity"/> into <see cref="Position"/>.</summary>
internal sealed class MovementSystem : ISystem
{
    public void Update(World world, double dt) =>
        world.Query<Position, Velocity>((Entity _, ref Position p, ref Velocity v) =>
        {
            p.X += v.X * (float)dt;
            p.Y += v.Y * (float)dt;
        });
}
