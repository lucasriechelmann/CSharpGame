using System.Numerics;

namespace Engine.Physics2D;

/// <summary>
/// A moving body: velocity, gravity response, and the contacts found by the last physics step. Needs a
/// <c>Transform</c> and a <see cref="BoxCollider"/> to be simulated by <see cref="PhysicsSystem"/>.
/// </summary>
public struct PhysicsBody
{
    /// <summary>Velocity in world units per second. Collisions zero the component that was blocked.</summary>
    public Vector2 Velocity;

    /// <summary>Multiplier on the world gravity (0 = floats, 1 = normal, above 1 = falls faster).</summary>
    public float GravityScale;

    /// <summary>Which sides are touching something solid after the last step (held while resting against a surface). Read by gameplay code (grounded checks, patrol turns).</summary>
    public CollisionFlags Contacts;

    /// <summary>True when the body is standing on something.</summary>
    public readonly bool IsGrounded => (Contacts & CollisionFlags.Below) != 0;

    /// <summary>Creates a body at rest that is affected by normal gravity.</summary>
    public static PhysicsBody Create() => new() { GravityScale = 1f };
}
