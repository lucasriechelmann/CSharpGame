namespace Engine.Audio;

/// <summary>Whether and how an <see cref="AudioSource"/> is placed in the world.</summary>
public enum AudioSpace
{
    /// <summary>Not positioned: the sound is the same wherever the listener is (music, interface sounds).</summary>
    Flat,

    /// <summary>Positioned by the entity's X and Y only, as in a 2D game; Z (the draw order) is ignored so it never counts as distance.</summary>
    Positional2D,

    /// <summary>Positioned by the entity's full world position.</summary>
    Positional3D,
}
