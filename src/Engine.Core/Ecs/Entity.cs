namespace Engine.Core.Ecs;

/// <summary>
/// Lightweight handle to an entity. The generation makes stale handles detectable
/// after the slot is recycled. Generation 0 is reserved for <see cref="Null"/>.
/// </summary>
public readonly record struct Entity(int Id, int Generation)
{
    public static Entity Null => default;

    public bool IsNull => Generation == 0;

    public override string ToString() => IsNull ? "Entity(null)" : $"Entity({Id}v{Generation})";
}
