using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;

namespace Engine.Physics2D;

/// <summary>
/// Slides every <see cref="MovingPlatform"/> along its path and carries the bodies standing on it by the same amount.
/// Run it in the fixed group <b>before</b> the systems that decide velocities and before <see cref="PhysicsSystem"/>, so bodies
/// are carried first and then collide against the platform's new position (and after <c>PreviousTransformSystem</c> so rendering
/// interpolates platform and riders over the same step).
/// </summary>
/// <remarks>
/// A body counts as a rider when the last physics step left it standing on something and its feet are at the platform's top, with
/// horizontal overlap; that test uses the platform's position before it moves. A platform that rises into the feet of a body that
/// was at or just above its top (a body landing on it) lifts that body to the new top, because a body left inside a solid would
/// fall straight through it (see the physics rules). Riders are shifted directly, not swept, so a platform can carry a rider into
/// a wall, and a platform moving into a body from the side or from above does not push it: design paths in open space.
/// </remarks>
public sealed class MovingPlatformSystem : ISystem
{
    /// <summary>How far above the platform's top a body's feet may be and still count as standing on it (float noise, far below a pixel).</summary>
    private const float RideTolerance = 0.1f;

    private readonly List<Move> _moves = new();
    private readonly EntityAction<Transform, MovingPlatform, BoxCollider> _movePlatform;
    private readonly EntityAction<Transform, PhysicsBody, BoxCollider> _carryRider;
    private float _dt;

    /// <summary>Creates the system.</summary>
    public MovingPlatformSystem()
    {
        _movePlatform = MovePlatform; // Created once so the per-step queries do not allocate delegates.
        _carryRider = CarryRider;
    }

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _dt = (float)dt;
        _moves.Clear();
        world.Query(_movePlatform);
        if (_moves.Count > 0) world.Query(_carryRider);
    }

    private void MovePlatform(Entity entity, ref Transform transform, ref MovingPlatform platform, ref BoxCollider collider)
    {
        float length = platform.Travel.Length();
        if (length <= 0f || platform.Speed <= 0f) return;

        var before = collider.GetBounds(transform.Position);
        float previous = platform.Distance;
        (platform.Distance, platform.Returning) = MovingPlatformMath.Advance(previous, platform.Returning, length, platform.Speed, _dt);

        var delta = platform.Travel / length * (platform.Distance - previous);
        transform.Position += new Vector3(delta, 0f);
        _moves.Add(new Move(before, delta));
    }

    private void CarryRider(Entity entity, ref Transform transform, ref PhysicsBody body, ref BoxCollider collider)
    {
        var box = collider.GetBounds(transform.Position);
        foreach (var move in _moves)
        {
            bool overlapsX = box.Min.X < move.Platform.Max.X && box.Max.X > move.Platform.Min.X;
            if (!overlapsX) continue;

            float oldTop = move.Platform.Max.Y;
            if (body.IsGrounded && MathF.Abs(box.Min.Y - oldTop) <= RideTolerance)
            {
                transform.Position += new Vector3(move.Delta, 0f);
                return;
            }

            // Rising into a body that was above (or resting on) the top: lift it so it is not left inside the platform.
            if (move.Delta.Y > 0f && box.Min.Y >= oldTop - RideTolerance && box.Min.Y < oldTop + move.Delta.Y)
            {
                transform.Position += new Vector3(0f, oldTop + move.Delta.Y - box.Min.Y, 0f);
                return;
            }
        }
    }

    /// <summary>A platform's box before it moved this step and how far it moved.</summary>
    private readonly record struct Move(Aabb Platform, Vector2 Delta);
}
