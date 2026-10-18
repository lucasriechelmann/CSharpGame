using Engine.Core.Assets;
using Engine.Core.Ecs;
using Engine.Rendering;

namespace Engine.Animation;

/// <summary>
/// Advances every <see cref="SpriteAnimator"/>: moves time forward, takes at most one state transition per update, and writes
/// the resulting frame into the entity's <see cref="Sprite.Source"/>. Run it in the per-frame group, after any system that
/// writes animator parameters and before rendering.
/// </summary>
public sealed class AnimationSystem : ISystem
{
    private readonly AssetManager _assets;
    private readonly EntityAction<SpriteAnimator, Sprite> _step;
    private float _dt;

    // Most animators share a machine, so remember the last lookup.
    private AssetHandle<AnimationStateMachine> _lastHandle;
    private AnimationStateMachine? _lastMachine;

    /// <summary>Creates the system.</summary>
    /// <param name="assets">Resolves each animator's state machine handle.</param>
    public AnimationSystem(AssetManager assets)
    {
        _assets = assets;
        _step = Step; // Created once so the per-frame query does not allocate a delegate.
    }

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _dt = (float)dt;
        _lastHandle = AssetHandle<AnimationStateMachine>.Invalid;
        _lastMachine = null;
        world.Query(_step);
    }

    private void Step(Entity entity, ref SpriteAnimator animator, ref Sprite sprite)
    {
        if (!animator.Machine.IsValid) return;

        var machine = Resolve(animator.Machine);
        if (animator.Parameters is null || animator.Parameters.Length < machine.ParameterCount)
            throw new InvalidOperationException(
                $"{entity} has an animator with {animator.Parameters?.Length ?? 0} parameters but its state machine needs {machine.ParameterCount}. Create it with SpriteAnimator.Create.");

        animator.StateTime += _dt * animator.Speed;

        var clip = machine.GetClip(animator.CurrentState);
        int frame = clip.GetFrameIndex(animator.StateTime, out bool finished);

        int next = machine.FindTransition(animator.CurrentState, animator.Parameters, finished);
        if (next >= 0)
        {
            animator.CurrentState = next;
            animator.StateTime = 0f;
            clip = machine.GetClip(next);
            frame = clip.GetFrameIndex(0f, out finished);
        }

        animator.Finished = finished;
        sprite.Source = clip.Frames[frame];
    }

    private AnimationStateMachine Resolve(AssetHandle<AnimationStateMachine> handle)
    {
        if (handle != _lastHandle)
        {
            _lastMachine = _assets.Get(handle);
            _lastHandle = handle;
        }

        return _lastMachine!;
    }
}
