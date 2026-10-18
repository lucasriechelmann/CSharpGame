using Engine.Core.Assets;

namespace Engine.Audio;

/// <summary>
/// Makes an entity play a sound. Plain data (saved in scenes); <see cref="AudioSourceSystem"/> does the playing, keeps a positioned
/// sound at its entity, and stops it when the entity or the component goes away. Create with <see cref="Create"/> because
/// <c>default</c> has zero volume and pitch.
/// </summary>
public struct AudioSource
{
    /// <summary>The sound to play.</summary>
    public AssetHandle<AudioClip> Clip;

    /// <summary>The sound's own volume (0 to 1 or more). Changes apply while it plays.</summary>
    public float Volume;

    /// <summary>Playback speed and pitch; 1 is normal.</summary>
    public float Pitch;

    /// <summary>Repeat until the entity is destroyed or the source is stopped.</summary>
    public bool Loop;

    /// <summary>Start playing the first time the system sees the entity. When false, start it with <see cref="AudioSourceSystem.Play"/>.</summary>
    public bool PlayOnStart;

    /// <summary>The mixer group to play through (for example "Music" or "Sfx"); empty means the master group.</summary>
    public string Group;

    /// <summary>Flat, or placed in the world by the entity's position. Positioning needs a mono clip.</summary>
    public AudioSpace Space;

    /// <summary>Within this distance a positioned sound is at full volume.</summary>
    public float MinDistance;

    /// <summary>Beyond this distance a positioned sound stops getting quieter.</summary>
    public float MaxDistance;

    /// <summary>How fast a positioned sound fades with distance; 1 is natural.</summary>
    public float Rolloff;

    /// <summary>A source that plays a clip once the system sees it: full volume, normal pitch, flat, master group.</summary>
    /// <param name="clip">The sound.</param>
    public static AudioSource Create(AssetHandle<AudioClip> clip) => new()
    {
        Clip = clip,
        Volume = 1f,
        Pitch = 1f,
        PlayOnStart = true,
        Group = string.Empty,
        Space = AudioSpace.Flat,
        MinDistance = 1f,
        MaxDistance = 50f,
        Rolloff = 1f,
    };
}
