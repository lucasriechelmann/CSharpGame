using Engine.Core.Assets;

namespace Engine.Animation;

/// <summary>Asset loader for <c>.anim.json</c> animation state machine files.</summary>
public sealed class AnimationStateMachineLoader : IAssetLoader<AnimationStateMachine>
{
    /// <inheritdoc />
    public AnimationStateMachine Load(string fullPath) => AnimationJson.Read(File.ReadAllText(fullPath));
}
