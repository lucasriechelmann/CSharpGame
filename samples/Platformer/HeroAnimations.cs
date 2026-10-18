using Engine.Animation;

namespace Platformer;

/// <summary>Builds the hero's animation state machine from <c>Assets/hero.png</c> (8 frames of 16x16 in one row).</summary>
public static class HeroAnimations
{
    /// <summary>Parameter: horizontal speed in world units per second (absolute value).</summary>
    public const string Speed = "Speed";

    /// <summary>Parameter: vertical velocity in world units per second (positive is up).</summary>
    public const string VelocityY = "VelocityY";

    /// <summary>Parameter: 1 when standing on something, otherwise 0.</summary>
    public const string Grounded = "Grounded";

    private const int Cell = 16;
    private const int SheetColumns = 8;

    /// <summary>Idle and run on the ground; jump while rising and fall while descending in the air.</summary>
    public static AnimationStateMachine Create()
    {
        var machine = new AnimationStateMachine();
        machine.AddParameter(Speed);
        machine.AddParameter(VelocityY);
        machine.AddParameter(Grounded);

        machine.AddState("Idle", Clip("Idle", [0, 1], 2.5f, AnimationLoopMode.PingPong));
        machine.AddState("Run", Clip("Run", [2, 3, 4, 5], 12f, AnimationLoopMode.Loop));
        machine.AddState("Jump", Clip("Jump", [6], 1f, AnimationLoopMode.Once));
        machine.AddState("Fall", Clip("Fall", [7], 1f, AnimationLoopMode.Once));

        var airborne = machine.Condition(Grounded, AnimationComparison.Equal, 0f);
        var grounded = machine.Condition(Grounded, AnimationComparison.Equal, 1f);

        // Any-state transitions, in priority order: the air states first, then the ground states.
        machine.AddTransition(null, "Jump", [airborne, machine.Condition(VelocityY, AnimationComparison.Greater, 0f)]);
        machine.AddTransition(null, "Fall", [airborne, machine.Condition(VelocityY, AnimationComparison.LessOrEqual, 0f)]);
        machine.AddTransition(null, "Run", [grounded, machine.Condition(Speed, AnimationComparison.Greater, 10f)]);
        machine.AddTransition(null, "Idle", [grounded, machine.Condition(Speed, AnimationComparison.LessOrEqual, 10f)]);
        return machine;
    }

    private static AnimationClip Clip(string name, int[] cells, float fps, AnimationLoopMode mode) =>
        AnimationClip.FromGrid(name, Cell, Cell, SheetColumns, cells, fps, mode);
}
