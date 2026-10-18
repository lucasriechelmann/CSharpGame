namespace Platformer;

/// <summary>
/// A flag the hero can touch to set where it respawns after losing a life. Plain data, placed in the scene file from
/// <c>checkpoint.prefab.json</c>; <see cref="Reached"/> is saved with the world, so a quick-load keeps raised flags raised.
/// </summary>
public struct Checkpoint
{
    /// <summary>True once the hero has touched this flag; the game shows the raised-flag art for it.</summary>
    public bool Reached;
}
