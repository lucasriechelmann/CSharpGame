using Engine.Core.Components;
using Engine.Core.Ecs;

namespace Engine.Runtime.Inspector;

/// <summary>The text tools show for an entity: its <see cref="Name"/> if it has one, always with its id so equal names stay distinguishable.</summary>
internal static class EntityLabel
{
    /// <summary>The label for <paramref name="entity"/>.</summary>
    /// <param name="world">The world holding the entity.</param>
    /// <param name="entity">The entity to describe.</param>
    public static string Of(World world, Entity entity) =>
        world.TryGet<Name>(entity, out var name) && !string.IsNullOrEmpty(name.Value)
            ? $"{name.Value}  #{entity.Id}"
            : $"Entity #{entity.Id}";
}
