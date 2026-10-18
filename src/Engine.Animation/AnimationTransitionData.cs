namespace Engine.Animation;

/// <summary>JSON shape of a transition between states, referring to states by name.</summary>
public sealed class AnimationTransitionData
{
    /// <summary>Source state name, or null (omitted) for "any state".</summary>
    public string? From { get; set; }

    /// <summary>Target state name.</summary>
    public string To { get; set; } = "";

    /// <summary>Wait for a once-clip to finish before the transition can fire.</summary>
    public bool RequiresFinished { get; set; }

    /// <summary>Tests that must all hold; empty means always.</summary>
    public List<AnimationConditionData> Conditions { get; set; } = new();
}
