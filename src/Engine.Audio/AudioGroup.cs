namespace Engine.Audio;

/// <summary>
/// One channel of the mixer, such as "Music" or "Sfx": a volume and a mute switch that apply to every sound played through it, and through
/// every group below it. The effective volume is the product of the volumes up the parent chain, so turning "Master" down turns everything
/// down and muting "Music" silences the music without touching the sound effects. Pure data, no device types.
/// </summary>
public sealed class AudioGroup
{
    private float _volume = 1f;

    internal AudioGroup(string name, AudioGroup? parent)
    {
        Name = name;
        Parent = parent;
    }

    /// <summary>The name the group is found by (case-insensitive, unique in its mixer).</summary>
    public string Name { get; }

    /// <summary>The group this one is mixed into, or null for the master group.</summary>
    public AudioGroup? Parent { get; }

    /// <summary>This group's own volume, 0 (silent) to 1 (full). Values outside the range are clamped.</summary>
    public float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>True silences the group and everything below it without losing <see cref="Volume"/>.</summary>
    public bool Muted { get; set; }

    /// <summary>The volume a sound in this group is actually played at: zero when this or any parent is muted, else the product of the volumes up the chain.</summary>
    public float EffectiveVolume
    {
        get
        {
            float volume = 1f;
            for (var group = this; group != null; group = group.Parent)
            {
                if (group.Muted) return 0f;
                volume *= group._volume;
            }

            return volume;
        }
    }
}
