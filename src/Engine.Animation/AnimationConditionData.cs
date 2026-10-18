namespace Engine.Animation;

/// <summary>JSON shape of a transition condition, referring to a parameter by name.</summary>
public sealed class AnimationConditionData
{
    /// <summary>Name of the parameter to test.</summary>
    public string Parameter { get; set; } = "";

    /// <summary>How to compare.</summary>
    public AnimationComparison Comparison { get; set; }

    /// <summary>The value to compare against (booleans are 0 and 1).</summary>
    public float Value { get; set; }
}
