using Engine.Core.Assets;

namespace Engine.Animation;

/// <summary>
/// Per-entity animation playback: which state of a shared <see cref="AnimationStateMachine"/> the entity is in, how long
/// it has been there, and the parameter values that drive transitions. <see cref="AnimationSystem"/> advances it and
/// writes the current frame into the entity's <c>Sprite.Source</c>, so the entity also needs a <c>Sprite</c>.
/// </summary>
public struct SpriteAnimator
{
    /// <summary>The state machine to play.</summary>
    public AssetHandle<AnimationStateMachine> Machine;

    /// <summary>Index of the current state in the machine.</summary>
    public int CurrentState;

    /// <summary>Seconds spent in the current state (scaled by <see cref="Speed"/>).</summary>
    public float StateTime;

    /// <summary>Playback speed multiplier: 1 normal, 2 twice as fast, 0 paused.</summary>
    public float Speed;

    /// <summary>True when the current clip is a <see cref="AnimationLoopMode.Once"/> clip that has played all its frames.</summary>
    public bool Finished;

    /// <summary>Parameter values, indexed as declared on the machine. Write them with <see cref="SetParameter(int, float)"/> from gameplay systems.</summary>
    public float[] Parameters;

    /// <summary>Creates an animator in the machine's default state with all parameters at zero.</summary>
    /// <param name="assets">Resolves the machine handle.</param>
    /// <param name="machine">Handle of the state machine to play.</param>
    /// <exception cref="InvalidOperationException">The machine has no states.</exception>
    public static SpriteAnimator Create(AssetManager assets, AssetHandle<AnimationStateMachine> machine)
    {
        var resolved = assets.Get(machine);
        if (resolved.StateCount == 0) throw new InvalidOperationException("The animation state machine has no states.");

        return new SpriteAnimator
        {
            Machine = machine,
            CurrentState = resolved.DefaultState,
            Speed = 1f,
            Parameters = new float[resolved.ParameterCount],
        };
    }

    /// <summary>Sets one parameter. Use the index from <see cref="AnimationStateMachine.GetParameterIndex"/>.</summary>
    public readonly void SetParameter(int index, float value) => Parameters[index] = value;

    /// <summary>Sets a boolean parameter (stored as 1 or 0).</summary>
    public readonly void SetParameter(int index, bool value) => Parameters[index] = value ? 1f : 0f;
}
