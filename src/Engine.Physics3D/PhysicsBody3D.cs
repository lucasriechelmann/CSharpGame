using System.Numerics;

namespace Engine.Physics3D;

/// <summary>
/// A moving body: velocity, gravity response, and the contacts found by the last physics step. Needs a <c>Transform</c> and a
/// <see cref="BoxCollider3D"/> to be simulated by <see cref="PhysicsSystem3D"/>.
/// </summary>
public struct PhysicsBody3D
{
    /// <summary>Velocity in world units per second. Collisions zero the component that was blocked.</summary>
    public Vector3 Velocity;

    /// <summary>Multiplier on the world gravity (0 = floats, 1 = normal, above 1 = falls faster).</summary>
    public float GravityScale;

    /// <summary>Which sides are touching something solid after the last step (held while resting against a surface).</summary>
    public CollisionFlags3D Contacts;

    /// <summary>True when the body is standing on something.</summary>
    public readonly bool IsGrounded => (Contacts & CollisionFlags3D.Below) != 0;

    /// <summary>Creates a body at rest that is affected by normal gravity.</summary>
    public static PhysicsBody3D Create() => new() { GravityScale = 1f };
}
