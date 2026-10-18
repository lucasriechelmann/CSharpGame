namespace Engine.Physics2D;

/// <summary>The pure back-and-forth arithmetic behind <see cref="MovingPlatform"/>, kept apart from the ECS so it is easy to test.</summary>
public static class MovingPlatformMath
{
    /// <summary>
    /// Advances a point travelling along a path of length <paramref name="length"/> and turns it around at either end. A step that
    /// overshoots an end is reflected (it travels the remaining distance back), so speed stays constant through the turn.
    /// </summary>
    /// <param name="distance">Current distance from the start, in 0..<paramref name="length"/>.</param>
    /// <param name="returning">True when heading back towards the start.</param>
    /// <param name="length">Path length; zero or less means no movement.</param>
    /// <param name="speed">Units per second; negative values are treated as zero.</param>
    /// <param name="dt">Step length in seconds.</param>
    /// <returns>The new distance and heading.</returns>
    public static (float Distance, bool Returning) Advance(float distance, bool returning, float length, float speed, float dt)
    {
        if (length <= 0f || speed <= 0f) return (Math.Clamp(distance, 0f, Math.Max(length, 0f)), returning);

        float next = distance + (returning ? -1f : 1f) * speed * dt;

        // A long step can cross more than one end (very short paths): keep reflecting until it fits.
        while (next < 0f || next > length)
        {
            if (next > length)
            {
                next = 2f * length - next;
                returning = true;
            }
            else
            {
                next = -next;
                returning = false;
            }
        }

        return (next, returning);
    }
}
