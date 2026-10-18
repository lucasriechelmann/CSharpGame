using Engine.Core.Components;
using Engine.Core.Ecs;

namespace Engine.Core;

/// <summary>
/// Records every <see cref="PreviousTransform"/> from its entity's current <see cref="Transform"/>. Add it as the
/// first fixed system: it must see the positions as they were before this step's movement, so that
/// previous = start of step and current = end of step.
/// </summary>
public sealed class PreviousTransformSystem : ISystem
{
    /// <inheritdoc />
    public void Update(World world, double dt) =>
        world.Query<Transform, PreviousTransform>(static (Entity _, ref Transform current, ref PreviousTransform previous) =>
            previous = PreviousTransform.From(current));
}
