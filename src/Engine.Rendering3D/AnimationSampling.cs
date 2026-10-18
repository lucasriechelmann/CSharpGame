using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>Pure keyframe maths: finding the two keys around a time and blending their values the way the channel asks.</summary>
public static class AnimationSampling
{
    /// <summary>
    /// Finds the keys on either side of <paramref name="time"/> and how far between them it is. Before the first key and after the last the
    /// value is held (both keys are the same one).
    /// </summary>
    /// <param name="times">Ascending key times; at least one.</param>
    /// <param name="time">The time to look up.</param>
    /// <param name="before">Index of the key at or before <paramref name="time"/>.</param>
    /// <param name="after">Index of the key after it (equal to <paramref name="before"/> when held).</param>
    /// <param name="blend">0 at <paramref name="before"/> to 1 at <paramref name="after"/>.</param>
    public static void FindKeys(ReadOnlySpan<float> times, float time, out int before, out int after, out float blend)
    {
        if (times.Length == 0) throw new ArgumentException("A channel needs at least one key.", nameof(times));

        if (time <= times[0])
        {
            before = after = 0;
            blend = 0f;
            return;
        }

        if (time >= times[^1])
        {
            before = after = times.Length - 1;
            blend = 0f;
            return;
        }

        // Binary search for the last key not after the time.
        int low = 0, high = times.Length - 1;
        while (high - low > 1)
        {
            int middle = (low + high) / 2;
            if (times[middle] <= time) low = middle;
            else high = middle;
        }

        before = low;
        after = high;
        blend = (time - times[low]) / (times[high] - times[low]);
    }

    /// <summary>The channel's translation or scale at a time.</summary>
    /// <param name="channel">A channel whose path is translation or scale.</param>
    /// <param name="time">The time in seconds.</param>
    public static Vector3 SampleVector3(ModelAnimationChannel channel, float time)
    {
        FindKeys(channel.Times, time, out int a, out int b, out float t);
        var values = channel.Values;
        switch (channel.Interpolation)
        {
            case AnimationInterpolation.Step:
                return Read3(values, a);
            case AnimationInterpolation.CubicSpline:
            {
                float dt = channel.Times[b] - channel.Times[a];
                return Hermite(Read3(values, a * 3 + 1), Read3(values, a * 3 + 2), Read3(values, b * 3 + 1), Read3(values, b * 3), dt, t);
            }

            default:
                return Vector3.Lerp(Read3(values, a), Read3(values, b), t);
        }
    }

    /// <summary>The channel's rotation at a time, as a unit quaternion.</summary>
    /// <param name="channel">A channel whose path is rotation.</param>
    /// <param name="time">The time in seconds.</param>
    public static Quaternion SampleQuaternion(ModelAnimationChannel channel, float time)
    {
        FindKeys(channel.Times, time, out int a, out int b, out float t);
        var values = channel.Values;
        switch (channel.Interpolation)
        {
            case AnimationInterpolation.Step:
                return Quaternion.Normalize(Read4(values, a));
            case AnimationInterpolation.CubicSpline:
            {
                float dt = channel.Times[b] - channel.Times[a];
                var result = Hermite(Read4(values, a * 3 + 1), Read4(values, a * 3 + 2), Read4(values, b * 3 + 1), Read4(values, b * 3), dt, t);
                return Quaternion.Normalize(result);
            }

            default:
                // Slerp takes the shorter arc between the two orientations, which is what animators intend.
                return Quaternion.Normalize(Quaternion.Slerp(Quaternion.Normalize(Read4(values, a)), Quaternion.Normalize(Read4(values, b)), t));
        }
    }

    private static Vector3 Read3(float[] v, int key) => new(v[key * 3], v[key * 3 + 1], v[key * 3 + 2]);

    private static Quaternion Read4(float[] v, int key) => new(v[key * 4], v[key * 4 + 1], v[key * 4 + 2], v[key * 4 + 3]);

    /// <summary>The cubic Hermite curve through two values with a tangent at each (the tangents are per second, so they scale by the key interval).</summary>
    private static Vector3 Hermite(Vector3 v0, Vector3 outTangent0, Vector3 v1, Vector3 inTangent1, float dt, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return (2f * t3 - 3f * t2 + 1f) * v0 + (t3 - 2f * t2 + t) * dt * outTangent0 + (-2f * t3 + 3f * t2) * v1 + (t3 - t2) * dt * inTangent1;
    }

    private static Quaternion Hermite(Quaternion v0, Quaternion outTangent0, Quaternion v1, Quaternion inTangent1, float dt, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        float h00 = 2f * t3 - 3f * t2 + 1f, h10 = (t3 - 2f * t2 + t) * dt, h01 = -2f * t3 + 3f * t2, h11 = (t3 - t2) * dt;
        return new Quaternion(
            h00 * v0.X + h10 * outTangent0.X + h01 * v1.X + h11 * inTangent1.X,
            h00 * v0.Y + h10 * outTangent0.Y + h01 * v1.Y + h11 * inTangent1.Y,
            h00 * v0.Z + h10 * outTangent0.Z + h01 * v1.Z + h11 * inTangent1.Z,
            h00 * v0.W + h10 * outTangent0.W + h01 * v1.W + h11 * inTangent1.W);
    }
}
