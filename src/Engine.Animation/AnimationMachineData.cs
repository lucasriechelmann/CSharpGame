namespace Engine.Animation;

/// <summary>JSON shape of a whole animation state machine (the contents of a <c>.anim.json</c> file).</summary>
public sealed class AnimationMachineData
{
    /// <summary>Parameter names, declared in index order.</summary>
    public List<string> Parameters { get; set; } = new();

    /// <summary>Name of the state animators start in; the first state when omitted.</summary>
    public string? Default { get; set; }

    /// <summary>The states.</summary>
    public List<AnimationStateData> States { get; set; } = new();

    /// <summary>The transitions in priority order (earlier wins).</summary>
    public List<AnimationTransitionData> Transitions { get; set; } = new();
}
