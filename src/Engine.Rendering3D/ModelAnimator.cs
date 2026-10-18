namespace Engine.Rendering3D;

/// <summary>
/// Plays one of the animation clips of the entity's <see cref="ModelRenderer"/> model: the clip's keyframes move the model's nodes, and skinned
/// parts deform with them. Plain data (it saves in scenes and the inspector edits it); <see cref="ModelAnimationSystem"/> advances it each frame.
/// </summary>
public struct ModelAnimator
{
    /// <summary>The clip to play, by the name it has in the model file; empty plays the first clip. A name the model lacks leaves the model at rest.</summary>
    public string Clip;

    /// <summary>Seconds into the clip. The system advances it while <see cref="Playing"/>; set it to scrub or restart.</summary>
    public float Time;

    /// <summary>Playback speed: 1 normal, 2 twice as fast, negative plays backwards.</summary>
    public float Speed;

    /// <summary>When true the clip repeats; when false it stops on its last frame.</summary>
    public bool Loop;

    /// <summary>False pauses on the current frame.</summary>
    public bool Playing;

    /// <summary>A looping animator at normal speed that starts playing at once.</summary>
    /// <param name="clip">The clip name, or empty for the first.</param>
    public static ModelAnimator Create(string clip = "") => new() { Clip = clip, Speed = 1f, Loop = true, Playing = true };
}
