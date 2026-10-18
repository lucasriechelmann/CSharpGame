namespace Engine.Animation;

/// <summary>What an <see cref="AnimationClip"/> does when it reaches its last frame.</summary>
public enum AnimationLoopMode
{
    /// <summary>Jump back to the first frame and keep going (walk and run cycles).</summary>
    Loop,

    /// <summary>Stop on the last frame and report the clip as finished (attacks, landings, deaths).</summary>
    Once,

    /// <summary>Play forwards, then backwards, then forwards again (breathing, bobbing). The end frames are not repeated.</summary>
    PingPong,
}
