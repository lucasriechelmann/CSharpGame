using Engine.Animation;
using Engine.Core.Assets;
using Engine.Core.Ecs;
using Engine.Rendering;

namespace Engine.Animation.Tests;

public sealed class AnimationSystemTests : IDisposable
{
    private const float Dt = 0.25f; // one frame at the clips' 4 fps

    private static readonly Rect IdleA = new(0, 0, 16, 16);
    private static readonly Rect IdleB = new(16, 0, 16, 16);
    private static readonly Rect RunA = new(32, 0, 16, 16);
    private static readonly Rect AttackA = new(48, 0, 16, 16);

    private readonly AssetManager _assets = new(Path.GetTempPath());
    private readonly World _world = new();
    private readonly AnimationSystem _system;
    private readonly AnimationStateMachine _machine = new();

    public AnimationSystemTests()
    {
        _system = new AnimationSystem(_assets);

        _machine.AddParameter("Moving");
        _machine.AddState("Idle", new AnimationClip("Idle", [IdleA, IdleB], 4f));
        _machine.AddState("Run", new AnimationClip("Run", [RunA], 4f));
        _machine.AddState("Attack", new AnimationClip("Attack", [AttackA], 4f, AnimationLoopMode.Once));
        _machine.AddTransition("Idle", "Run", [_machine.Condition("Moving", AnimationComparison.Equal, 1f)]);
        _machine.AddTransition("Run", "Idle", [_machine.Condition("Moving", AnimationComparison.Equal, 0f)]);
        _machine.AddTransition("Attack", "Idle", requiresFinished: true);
    }

    public void Dispose() => _assets.Dispose();

    private AssetHandle<AnimationStateMachine> MachineHandle => _handle ??= _assets.Add("anim/test", _machine);

    private AssetHandle<AnimationStateMachine>? _handle;

    private Entity Spawn(string? startState = null)
    {
        var animator = SpriteAnimator.Create(_assets, MachineHandle);
        if (startState is not null) animator.CurrentState = _machine.GetStateIndex(startState);

        var entity = _world.CreateEntity();
        _world.Add(entity, animator);
        _world.Add(entity, new Sprite());
        return entity;
    }

    private void Step(int count = 1)
    {
        for (int i = 0; i < count; i++) _system.Update(_world, Dt);
    }

    [Fact]
    public void Writes_the_current_frame_into_the_sprite_source()
    {
        var e = Spawn();

        Step(); // 0.25 s: second Idle frame

        Assert.Equal(IdleB, _world.Get<Sprite>(e).Source);
    }

    [Fact]
    public void The_clip_loops_over_time()
    {
        var e = Spawn();

        Step(2); // 0.5 s: back to the first frame

        Assert.Equal(IdleA, _world.Get<Sprite>(e).Source);
    }

    [Fact]
    public void Parameters_drive_state_changes_and_restart_the_clip()
    {
        var e = Spawn();
        _world.Get<SpriteAnimator>(e).SetParameter(_machine.GetParameterIndex("Moving"), true);

        Step();

        var animator = _world.Get<SpriteAnimator>(e);
        Assert.Equal(_machine.GetStateIndex("Run"), animator.CurrentState);
        Assert.Equal(0f, animator.StateTime);
        Assert.Equal(RunA, _world.Get<Sprite>(e).Source);
    }

    [Fact]
    public void A_state_can_be_left_again_when_the_parameter_changes_back()
    {
        var e = Spawn();
        int moving = _machine.GetParameterIndex("Moving");

        _world.Get<SpriteAnimator>(e).SetParameter(moving, 1f);
        Step();
        _world.Get<SpriteAnimator>(e).SetParameter(moving, 0f);
        Step();

        Assert.Equal(_machine.GetStateIndex("Idle"), _world.Get<SpriteAnimator>(e).CurrentState);
    }

    [Fact]
    public void Once_clips_report_finished_and_can_hand_over_to_the_next_state()
    {
        var e = Spawn(startState: "Attack");

        Step(); // 0.25 s: the single-frame attack is complete

        Assert.Equal(_machine.GetStateIndex("Idle"), _world.Get<SpriteAnimator>(e).CurrentState);
        Assert.Equal(IdleA, _world.Get<Sprite>(e).Source);
    }

    [Fact]
    public void Speed_scales_playback()
    {
        var e = Spawn();
        _world.Get<SpriteAnimator>(e).Speed = 2f;

        Step(); // 0.5 s of animation time: wrapped to frame 0 again

        Assert.Equal(IdleA, _world.Get<Sprite>(e).Source);
        Assert.Equal(0.5f, _world.Get<SpriteAnimator>(e).StateTime);
    }

    [Fact]
    public void A_paused_animator_does_not_advance()
    {
        var e = Spawn();
        _world.Get<SpriteAnimator>(e).Speed = 0f;

        Step(5);

        Assert.Equal(0f, _world.Get<SpriteAnimator>(e).StateTime);
        Assert.Equal(IdleA, _world.Get<Sprite>(e).Source);
    }

    [Fact]
    public void Entities_sharing_a_machine_animate_independently()
    {
        var a = Spawn();
        var b = Spawn();
        _world.Get<SpriteAnimator>(b).SetParameter(_machine.GetParameterIndex("Moving"), true);

        Step();

        Assert.Equal(IdleB, _world.Get<Sprite>(a).Source);
        Assert.Equal(RunA, _world.Get<Sprite>(b).Source);
    }

    [Fact]
    public void An_animator_with_too_few_parameters_fails_loudly()
    {
        var e = _world.CreateEntity();
        _world.Add(e, new SpriteAnimator { Machine = MachineHandle, Speed = 1f }); // not created via Create: no parameter array
        _world.Add(e, new Sprite());

        Assert.Throws<InvalidOperationException>(() => Step());
    }

    [Fact]
    public void Animators_without_a_machine_are_skipped()
    {
        var e = _world.CreateEntity();
        _world.Add(e, new SpriteAnimator());
        _world.Add(e, new Sprite { Source = IdleB });

        Step();

        Assert.Equal(IdleB, _world.Get<Sprite>(e).Source);
    }

    [Fact]
    public void Creating_an_animator_for_an_empty_machine_is_rejected()
    {
        var handle = _assets.Add("anim/empty", new AnimationStateMachine());

        Assert.Throws<InvalidOperationException>(() => SpriteAnimator.Create(_assets, handle));
    }
}
