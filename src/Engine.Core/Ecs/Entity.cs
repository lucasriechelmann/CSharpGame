namespace Engine.Core.Ecs;

/// <summary>
/// Lightweight handle to an entity. The generation makes stale handles detectable
/// after the slot is recycled. Generation 0 is reserved for <see cref="Null"/>.
/// </summary>
/// <param name="Id">Slot index of the entity. Reused after the entity is destroyed.</param>
/// <param name="Generation">How many times the slot has been recycled; a handle is only valid while it matches.</param>
public readonly record struct Entity(int Id, int Generation)
{
    /// <summary>The "no entity" handle. It is never alive.</summary>
    public static Entity Null => default;

    /// <summary>True when this is the <see cref="Null"/> handle.</summary>
    public bool IsNull => Generation == 0;

    /// <inheritdoc />
    public override string ToString() => IsNull ? "Entity(null)" : $"Entity({Id}v{Generation})";
}
