namespace Engine.Animation;

/// <summary>JSON shape of one animation state: a name and the clip it plays.</summary>
public sealed class AnimationStateData
{
    /// <summary>State name, referenced by transitions.</summary>
    public string Name { get; set; } = "";

    /// <summary>The clip this state plays.</summary>
    public AnimationClipData Clip { get; set; } = new();
}
