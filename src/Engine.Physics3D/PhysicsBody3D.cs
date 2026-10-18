using System.Numerics;

namespace Engine.Physics3D;

/// <summary>
/// A moving body: velocity, gravity response, mass, and the contacts found by the last physics step. Needs a <c>Transform</c> and a
/// <see cref="BoxCollider3D"/> or <see cref="CapsuleCollider3D"/> to be simulated by <see cref="PhysicsSystem3D"/>. Bodies collide with
/// each other: a pushed body takes on speed in proportion to the pusher's mass, and one body can rest on another.
/// </summary>
public struct PhysicsBody3D
{
    /// <summary>Velocity in world units per second. Collisions zero the component that was blocked.</summary>
    public Vector3 Velocity;

    /// <summary>Multiplier on the world gravity (0 = floats, 1 = normal, above 1 = falls faster).</summary>
    public float GravityScale;

    /// <summary>
    /// How heavy the body is, for collisions between bodies (kilograms by convention, but only ratios matter). A heavy body barely slows
    /// when it pushes a light one. Zero or negative means 1, so a body from older data (which has no mass field) still behaves.
    /// </summary>
    public float Mass;

    /// <summary>Which sides are touching something solid after the last step (held while resting against a surface).</summary>
    public CollisionFlags3D Contacts;

    /// <summary>True when the body is standing on something.</summary>
    public readonly bool IsGrounded => (Contacts & CollisionFlags3D.Below) != 0;

    /// <summary>The mass used in collisions: <see cref="Mass"/>, or 1 when it is not positive.</summary>
    public readonly float EffectiveMass => Mass > 0f ? Mass : 1f;

    /// <summary>Creates a body at rest that is affected by normal gravity and weighs 1.</summary>
    public static PhysicsBody3D Create() => new() { GravityScale = 1f, Mass = 1f };
}
