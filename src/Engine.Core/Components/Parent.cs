using Engine.Core.Ecs;

namespace Engine.Core.Components;

/// <summary>
/// Makes the entity a child of another: its <see cref="Transform"/> is then <b>local</b> (relative to <see cref="Value"/>), and the
/// world placement is the local matrix times the parent's world matrix. Entities without a <see cref="Parent"/> keep a world-space
/// <see cref="Transform"/>, exactly as before, so nothing changes for flat scenes.
/// </summary>
/// <remarks>
/// Set it through <see cref="Hierarchy.TransformHierarchy.SetParent"/>, which refuses cycles and can keep the world placement. It is
/// deliberately not a registered component: scenes store the link as a <c>"parent"</c> index on the entity entry, because an
/// <see cref="Entity"/> handle means nothing in a file. A parent that is destroyed counts as "no parent" (the child then reads its
/// local transform as a world one); <see cref="Hierarchy.TransformHierarchy.Destroy"/> removes a whole subtree instead.
/// </remarks>
public struct Parent
{
    /// <summary>The parent entity.</summary>
    public Entity Value;
}
