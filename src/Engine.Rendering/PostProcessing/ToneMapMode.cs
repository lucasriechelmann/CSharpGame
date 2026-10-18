namespace Engine.Rendering;

/// <summary>The curve a <see cref="ToneMapEffect"/> uses to squeeze unlimited brightness into the 0..1 range a screen can show.</summary>
public enum ToneMapMode
{
    /// <summary>Simple and soft: <c>c / (1 + c)</c>. Never clips, but dulls the mid-tones.</summary>
    Reinhard,

    /// <summary>A film-like S-curve (Narkowicz's fit of the ACES curve): punchy contrast, bright colours roll off to white.</summary>
    Aces,

    /// <summary>No curve, just a clamp to 0..1: what a screen does anyway. Useful to compare against the others.</summary>
    Clamp,
}
