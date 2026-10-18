using System.Numerics;

namespace Engine.Physics3D;

/// <summary>Pure helpers for <see cref="CharacterControllerSystem3D"/>, separate so the arithmetic is tested without a world.</summary>
public static class CharacterMath
{
    /// <summary>
    /// Moves <paramref name="current"/> towards <paramref name="target"/> by at most <paramref name="maxDelta"/>, in a straight line, and
    /// stops exactly on the target instead of overshooting it.
    /// </summary>
    /// <param name="current">The present horizontal velocity.</param>
    /// <param name="target">The wanted horizontal velocity.</param>
    /// <param name="maxDelta">The largest change allowed this step (acceleration times time).</param>
    public static Vector2 MoveToward(Vector2 current, Vector2 target, float maxDelta)
    {
        var difference = target - current;
        float distance = difference.Length();
        if (distance <= maxDelta || distance < 1e-6f) return target;

        return current + difference / distance * maxDelta;
    }

    /// <summary>The wanted horizontal velocity for an input: its direction at <paramref name="speed"/>, with input longer than 1 clamped so diagonals are not faster.</summary>
    /// <param name="move">The input vector.</param>
    /// <param name="speed">Speed at full input.</param>
    public static Vector2 TargetVelocity(Vector2 move, float speed)
    {
        if (move.LengthSquared() > 1f) move = Vector2.Normalize(move);
        return move * speed;
    }
}
