namespace Engine.Core.Components;

/// <summary>A human-readable label for an entity, used by tools, the inspector and saved scenes. It has no effect on gameplay.</summary>
public struct Name
{
    /// <summary>The label.</summary>
    public string Value;

    /// <summary>Creates a name component.</summary>
    public static Name Of(string value) => new() { Value = value };
}
