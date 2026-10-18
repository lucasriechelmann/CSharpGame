namespace Engine.Core;

/// <summary>Small scalar maths helpers the BCL does not provide.</summary>
public static class MathHelper
{
    /// <summary>
    /// Moves <paramref name="current"/> towards <paramref name="target"/> by at most <paramref name="maxDelta"/>
    /// without overshooting. Useful for acceleration and deceleration.
    /// </summary>
    /// <param name="current">Starting value.</param>
    /// <param name="target">Value to approach.</param>
    /// <param name="maxDelta">Largest change allowed (must not be negative).</param>
    public static float MoveToward(float current, float target, float maxDelta)
    {
        if (maxDelta < 0f) throw new ArgumentOutOfRangeException(nameof(maxDelta), "maxDelta must not be negative.");

        float difference = target - current;
        return MathF.Abs(difference) <= maxDelta ? target : current + MathF.Sign(difference) * maxDelta;
    }
}
