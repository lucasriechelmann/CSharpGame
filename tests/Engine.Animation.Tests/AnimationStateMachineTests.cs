using Engine.Animation;
using Engine.Rendering;

namespace Engine.Animation.Tests;

public class AnimationStateMachineTests
{
    private static AnimationClip Clip(string name, AnimationLoopMode mode = AnimationLoopMode.Loop) =>
        new(name, [new Rect(0, 0, 16, 16)], 4f, mode);

    /// <summary>States Idle, Run, Jump; parameters Speed (0) and Grounded (1).</summary>
    private static AnimationStateMachine NewMachine()
    {
        var machine = new AnimationStateMachine();
        machine.AddParameter("Speed");
        machine.AddParameter("Grounded");
        machine.AddState("Idle", Clip("Idle"));
        machine.AddState("Run", Clip("Run"));
        machine.AddState("Jump", Clip("Jump"));
        return machine;
    }

    [Fact]
    public void Parameters_and_states_are_indexed_in_declaration_order()
    {
        var machine = NewMachine();

        Assert.Equal(0, machine.GetParameterIndex("Speed"));
        Assert.Equal(1, machine.GetParameterIndex("Grounded"));
        Assert.Equal(2, machine.ParameterCount - 0);
        Assert.Equal(0, machine.GetStateIndex("Idle"));
        Assert.Equal(2, machine.GetStateIndex("Jump"));
        Assert.Equal("Run", machine.GetStateName(1));
        Assert.Equal("Run", machine.GetClip(1).Name);
        Assert.Equal(3, machine.StateCount);
    }

    [Fact]
    public void Unknown_and_duplicate_names_fail_loudly()
    {
        var machine = NewMachine();

        Assert.Throws<KeyNotFoundException>(() => machine.GetParameterIndex("Nope"));
        Assert.Throws<KeyNotFoundException>(() => machine.GetStateIndex("Nope"));
        Assert.Throws<ArgumentException>(() => machine.AddParameter("Speed"));
        Assert.Throws<ArgumentException>(() => machine.AddState("Idle", Clip("again")));
        Assert.Throws<KeyNotFoundException>(() => machine.AddTransition("Idle", "Nope"));
        Assert.Throws<KeyNotFoundException>(() => machine.AddTransition("Nope", "Idle"));
    }

    [Fact]
    public void A_transition_fires_when_its_conditions_hold()
    {
        var machine = NewMachine();
        machine.AddTransition("Idle", "Run", [machine.Condition("Speed", AnimationComparison.Greater, 10f)]);

        Assert.Equal(-1, machine.FindTransition(state: 0, new float[] { 5f, 1f }, finished: false));
        Assert.Equal(1, machine.FindTransition(state: 0, new float[] { 50f, 1f }, finished: false));
    }

    [Fact]
    public void All_conditions_must_hold()
    {
        var machine = NewMachine();
        machine.AddTransition("Idle", "Run", [
            machine.Condition("Speed", AnimationComparison.Greater, 10f),
            machine.Condition("Grounded", AnimationComparison.Equal, 1f),
        ]);

        Assert.Equal(-1, machine.FindTransition(0, new float[] { 50f, 0f }, false)); // fast but airborne
        Assert.Equal(1, machine.FindTransition(0, new float[] { 50f, 1f }, false));
    }

    [Fact]
    public void A_transition_only_applies_in_its_source_state()
    {
        var machine = NewMachine();
        machine.AddTransition("Idle", "Run");

        Assert.Equal(1, machine.FindTransition(0, new float[] { 0f, 0f }, false));
        Assert.Equal(-1, machine.FindTransition(2, new float[] { 0f, 0f }, false)); // in Jump
    }

    [Fact]
    public void Any_state_transitions_apply_everywhere_except_to_themselves()
    {
        var machine = NewMachine();
        machine.AddTransition(null, "Jump", [machine.Condition("Grounded", AnimationComparison.Equal, 0f)]);
        var airborne = new float[] { 0f, 0f };

        Assert.Equal(2, machine.FindTransition(0, airborne, false));
        Assert.Equal(2, machine.FindTransition(1, airborne, false));
        Assert.Equal(-1, machine.FindTransition(2, airborne, false)); // already in Jump: do not restart it
    }

    [Fact]
    public void Earlier_transitions_have_priority()
    {
        var machine = NewMachine();
        machine.AddTransition("Idle", "Jump");  // always true, added first
        machine.AddTransition("Idle", "Run");

        Assert.Equal(2, machine.FindTransition(0, new float[] { 0f, 0f }, false));
    }

    [Fact]
    public void Requires_finished_waits_for_the_clip_to_end()
    {
        var machine = new AnimationStateMachine();
        machine.AddState("Attack", Clip("Attack", AnimationLoopMode.Once));
        machine.AddState("Idle", Clip("Idle"));
        machine.AddTransition("Attack", "Idle", requiresFinished: true);

        Assert.Equal(-1, machine.FindTransition(0, [], finished: false));
        Assert.Equal(1, machine.FindTransition(0, [], finished: true));
    }

    [Theory]
    [InlineData(AnimationComparison.Equal, 5f, true)]
    [InlineData(AnimationComparison.Equal, 4f, false)]
    [InlineData(AnimationComparison.NotEqual, 4f, true)]
    [InlineData(AnimationComparison.NotEqual, 5f, false)]
    [InlineData(AnimationComparison.Greater, 4f, true)]
    [InlineData(AnimationComparison.Greater, 5f, false)]
    [InlineData(AnimationComparison.GreaterOrEqual, 5f, true)]
    [InlineData(AnimationComparison.GreaterOrEqual, 6f, false)]
    [InlineData(AnimationComparison.Less, 6f, true)]
    [InlineData(AnimationComparison.Less, 5f, false)]
    [InlineData(AnimationComparison.LessOrEqual, 5f, true)]
    [InlineData(AnimationComparison.LessOrEqual, 4f, false)]
    public void Conditions_compare_the_parameter_with_the_value(AnimationComparison comparison, float value, bool expected)
    {
        var condition = new AnimationCondition(0, comparison, value);

        Assert.Equal(expected, condition.Evaluate(new float[] { 5f }));
    }

    [Fact]
    public void Conditions_on_undeclared_parameters_are_rejected_when_added()
    {
        var machine = NewMachine();

        Assert.Throws<ArgumentException>(() =>
            machine.AddTransition("Idle", "Run", [new AnimationCondition(7, AnimationComparison.Equal, 0f)]));
    }
}
