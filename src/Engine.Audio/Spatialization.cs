namespace Engine.Audio;

/// <summary>
/// How a positioned sound gets quieter with distance (the "inverse distance clamped" model, which is also what OpenAL is set to, so
/// this maths and the device agree): full volume inside <see cref="MinDistance"/>, falling off with the inverse of distance beyond it,
/// and no further change past <see cref="MaxDistance"/>.
/// </summary>
/// <param name="MinDistance">Distance up to which the sound is at full volume (world units).</param>
/// <param name="MaxDistance">Distance beyond which the sound stops getting quieter.</param>
/// <param name="Rolloff">How fast it fades: 1 is natural, 0 means no fading, larger fades faster.</param>
public readonly record struct Spatialization(float MinDistance = 1f, float MaxDistance = 50f, float Rolloff = 1f)
{
    /// <summary>The default: full volume within 1 unit, fading naturally out to 50 units.</summary>
    public static Spatialization Default => new(1f, 50f, 1f);

    /// <summary>The volume factor (0 to 1) for a listener this far from the source.</summary>
    /// <param name="distance">Distance in world units; negative values count as 0.</param>
    public float Attenuation(float distance)
    {
        float min = Math.Max(MinDistance, 0.0001f);
        float clamped = Math.Clamp(distance, min, Math.Max(MaxDistance, min));
        return min / (min + Math.Max(Rolloff, 0f) * (clamped - min));
    }

    /// <summary>Checks the numbers make sense.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A distance is not positive, the maximum is below the minimum, or the rolloff is negative.</exception>
    public void Validate()
    {
        if (MinDistance <= 0f) throw new ArgumentOutOfRangeException(nameof(MinDistance), MinDistance, "MinDistance must be positive.");
        if (MaxDistance < MinDistance) throw new ArgumentOutOfRangeException(nameof(MaxDistance), MaxDistance, "MaxDistance must not be below MinDistance.");
        if (Rolloff < 0f) throw new ArgumentOutOfRangeException(nameof(Rolloff), Rolloff, "Rolloff must not be negative.");
    }
}
