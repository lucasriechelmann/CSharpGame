using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Input;

namespace Platformer;

/// <summary>
/// Reads the movement axes once per frame and latches them into the player's <see cref="Velocity"/>. Fixed-step
/// systems then integrate that velocity, so input is never read (or an edge double-counted) inside the fixed loop.
/// </summary>
/// <param name="actions">The action map to read movement from.</param>
public sealed class PlayerInputSystem(ActionMap actions) : ISystem
{
    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        var direction = new Vector3(actions.GetAxis(GameActions.MoveX), actions.GetAxis(GameActions.MoveY), 0f);
        if (direction.LengthSquared() > 1f)
            direction = Vector3.Normalize(direction); // no faster along diagonals

        world.Query<PlayerControlled, Velocity>((Entity _, ref PlayerControlled player, ref Velocity velocity) =>
            velocity.Value = direction * player.Speed);
    }
}
