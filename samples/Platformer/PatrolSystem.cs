using Engine.Core.Ecs;
using Engine.Physics2D;

namespace Platformer;

/// <summary>Walks <see cref="Patrol"/> bodies and turns them around at walls. Run it in the fixed group before <see cref="PhysicsSystem"/>.</summary>
public sealed class PatrolSystem : ISystem
{
    /// <inheritdoc />
    public void Update(World world, double dt) =>
        world.Query<Patrol, PhysicsBody>((Entity _, ref Patrol patrol, ref PhysicsBody body) =>
        {
            // Contacts come from the previous physics step and persist while touching, so this turns exactly once per wall.
            if ((body.Contacts & CollisionFlags.Right) != 0) patrol.Direction = -1f;
            else if ((body.Contacts & CollisionFlags.Left) != 0) patrol.Direction = 1f;

            body.Velocity.X = patrol.Direction * patrol.Speed;
        });
}
