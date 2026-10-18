namespace Engine.Rendering3D;

/// <summary>One animation clip of a model: a name, how long it runs, and the keyframe channels that move its nodes.</summary>
/// <param name="Name">The clip's name from the file, or "Animation N" when it has none.</param>
/// <param name="Duration">Length in seconds: the time of the last keyframe of any channel.</param>
/// <param name="Channels">The channels; nodes with no channel keep their rest transform.</param>
public sealed record ModelAnimationData(string Name, float Duration, IReadOnlyList<ModelAnimationChannel> Channels);
