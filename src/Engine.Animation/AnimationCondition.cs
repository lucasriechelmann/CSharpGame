namespace Engine.Animation;

/// <summary>One test on an animator parameter, such as "Speed is greater than 10". Transitions fire when all of their conditions hold.</summary>
/// <param name="Parameter">Index of the parameter in the animator's parameter array (see <see cref="AnimationStateMachine.GetParameterIndex"/>).</param>
/// <param name="Comparison">How to compare.</param>
/// <param name="Value">What to compare against. Booleans are stored as 0 (false) and 1 (true).</param>
public readonly record struct AnimationCondition(int Parameter, AnimationComparison Comparison, float Value)
{
    /// <summary>Evaluates the condition against the current parameter values.</summary>
    public bool Evaluate(ReadOnlySpan<float> parameters)
    {
        float actual = parameters[Parameter];
        return Comparison switch
        {
            AnimationComparison.Equal => actual == Value,
            AnimationComparison.NotEqual => actual != Value,
            AnimationComparison.Greater => actual > Value,
            AnimationComparison.GreaterOrEqual => actual >= Value,
            AnimationComparison.Less => actual < Value,
            AnimationComparison.LessOrEqual => actual <= Value,
            _ => throw new InvalidOperationException($"Unknown comparison {Comparison}."),
        };
    }
}
