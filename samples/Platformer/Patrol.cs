namespace Platformer;

/// <summary>Makes a physics body walk back and forth, turning around when it touches a wall.</summary>
public struct Patrol
{
    /// <summary>Walking speed in world units per second.</summary>
    public float Speed;

    /// <summary>Current walking direction: +1 right, -1 left.</summary>
    public float Direction;
}
