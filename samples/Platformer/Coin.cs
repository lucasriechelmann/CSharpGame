namespace Platformer;

/// <summary>A collectible: touching it with the hero removes the entity and adds <see cref="Value"/> to the score. Plain data, so coins are placed in the scene file.</summary>
public struct Coin
{
    /// <summary>Score awarded when collected.</summary>
    public int Value;
}
