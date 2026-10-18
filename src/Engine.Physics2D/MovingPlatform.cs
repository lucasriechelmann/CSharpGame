using System.Numerics;

namespace Engine.Physics2D;

/// <summary>
/// Makes a solid box slide back and forth along a straight path, carrying whatever stands on it. Add it to an entity that
/// already has a <c>Transform</c>, a <see cref="BoxCollider"/> and a <see cref="StaticSolid"/> tag; <see cref="MovingPlatformSystem"/>
/// moves it. The path's start is wherever the entity is placed, so a scene file needs only <see cref="Travel"/> and <see cref="Speed"/>.
/// </summary>
/// <remarks>
/// Every field is plain data and <c>default</c> is valid (a platform that does not move yet), so it saves and loads in
/// scenes and edits in the inspector. <see cref="Distance"/> and <see cref="Returning"/> are runtime state that is saved with
/// the world, so a quick-load puts the platform back where it was in its cycle.
/// </remarks>
public struct MovingPlatform
{
    /// <summary>Displacement from the start of the path to its far end, in world units. Zero means the platform stays put.</summary>
    public Vector2 Travel;

    /// <summary>Speed along the path in world units per second.</summary>
    public float Speed;

    /// <summary>How far along the path the platform currently is: 0 at the start, <c>Travel.Length()</c> at the far end.</summary>
    public float Distance;

    /// <summary>True while heading back from the far end to the start.</summary>
    public bool Returning;
}
