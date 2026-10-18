namespace Engine.Audio;

/// <summary>
/// Handle to one playing sound. A device has a fixed number of voices that are reused, so the generation tells a stale handle
/// (its sound already finished and the voice was reused) apart from a live one, like an <c>Entity</c> handle.
/// </summary>
/// <param name="Index">Which voice slot in the device.</param>
/// <param name="Generation">How many sounds that slot has started; 0 is reserved for <see cref="None"/>.</param>
public readonly record struct VoiceId(int Index, int Generation)
{
    /// <summary>"No sound": what a silent device returns and what <c>default</c> gives you. Never playing.</summary>
    public static VoiceId None => default;

    /// <summary>True when this is <see cref="None"/>.</summary>
    public bool IsNone => Generation == 0;
}
