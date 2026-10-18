using Engine.Core.Assets;
using Engine.Core.Ecs;

namespace Engine.Rendering3D;

/// <summary>
/// Advances every <see cref="ModelAnimator"/> and poses its model: samples the chosen clip at the animator's time into the entity's
/// <see cref="AnimatedPose"/> (kept in the shared <see cref="ModelPoses"/>), which <see cref="MeshRenderSystem"/> then draws with. A frame
/// system: add it to <c>FrameSystems</c>, so animation stops with a paused game and the frozen editor, and runs before the render systems.
/// </summary>
public sealed class ModelAnimationSystem : ISystem
{
    private const int PruneEvery = 120;

    private readonly AssetManager _assets;
    private readonly ModelPoses _poses;
    private readonly EntityAction<ModelRenderer, ModelAnimator> _animate;
    private float _dt;
    private int _frames;

    /// <summary>Creates the system.</summary>
    /// <param name="assets">Resolves the model handles.</param>
    /// <param name="poses">Where the poses are written; give the same instance to <see cref="MeshRenderSystem"/>.</param>
    public ModelAnimationSystem(AssetManager assets, ModelPoses poses)
    {
        _assets = assets;
        _poses = poses;
        _animate = Animate; // created once so the per-frame query does not allocate a delegate
    }

    /// <inheritdoc />
    public void Update(Engine.Core.Ecs.World world, double dt)
    {
        _dt = (float)dt;
        world.Query(_animate);
        if (++_frames % PruneEvery == 0) _poses.Prune(world);
    }

    private void Animate(Entity entity, ref ModelRenderer renderer, ref ModelAnimator animator)
    {
        if (!renderer.Model.IsValid || !_assets.TryGet(renderer.Model, out var model)) return;
        if (model.Animations.Count == 0 && model.Skins.Count == 0 && model.Nodes.Count == 0) return;

        var pose = _poses.GetOrCreate(entity, model);
        var clip = FindClip(model, animator.Clip);
        bool finished = false;
        if (clip != null && animator.Playing) animator.Time = Advance(animator.Time, _dt * animator.Speed, clip.Duration, animator.Loop, out finished);

        if (finished) animator.Playing = false;
        pose.Evaluate(clip, animator.Time);
    }

    /// <summary>The clip with the given name; the first clip when the name is empty; null when there is no such clip.</summary>
    /// <param name="model">The model.</param>
    /// <param name="name">The wanted clip name, or empty for the first.</param>
    public static ModelAnimationData? FindClip(Model model, string? name)
    {
        if (model.Animations.Count == 0) return null;
        if (string.IsNullOrEmpty(name)) return model.Animations[0];

        for (int i = 0; i < model.Animations.Count; i++)
        {
            if (model.Animations[i].Name == name) return model.Animations[i];
        }

        return null;
    }

    /// <summary>
    /// Moves the playhead: looping wraps it into the clip (also when playing backwards), otherwise it stops on the first or last frame and
    /// reports that the clip finished.
    /// </summary>
    /// <param name="time">The current time in seconds.</param>
    /// <param name="delta">How far to move (seconds times speed; negative plays backwards).</param>
    /// <param name="duration">The clip length in seconds.</param>
    /// <param name="loop">Whether to wrap.</param>
    /// <param name="finished">True when a non-looping clip hit its end.</param>
    public static float Advance(float time, float delta, float duration, bool loop, out bool finished)
    {
        finished = false;
        float next = time + delta;
        if (duration <= 0f) return 0f;

        if (loop)
        {
            next %= duration;
            return next < 0f ? next + duration : next;
        }

        if (next >= duration || next <= 0f)
        {
            finished = delta != 0f;
            return Math.Clamp(next, 0f, duration);
        }

        return next;
    }
}
