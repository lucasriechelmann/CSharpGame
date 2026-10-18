namespace Engine.Animation;

/// <summary>A possible move from one animation state to another, taken when every condition holds.</summary>
/// <param name="From">Index of the source state, or <see cref="AnyState"/> for "from whatever state we are in".</param>
/// <param name="To">Index of the target state.</param>
/// <param name="RequiresFinished">When true the current clip must have finished (only <see cref="AnimationLoopMode.Once"/> clips ever finish) before the transition can fire.</param>
/// <param name="Conditions">Tests on the parameters; all must hold. Empty means "always".</param>
public readonly record struct AnimationTransition(int From, int To, bool RequiresFinished, AnimationCondition[] Conditions)
{
    /// <summary>Value of <see cref="From"/> that matches every state.</summary>
    public const int AnyState = -1;

    /// <summary>True when the transition applies to <paramref name="state"/> and all of its requirements are met.</summary>
    public bool IsSatisfied(int state, ReadOnlySpan<float> parameters, bool finished)
    {
        if (From != AnyState && From != state) return false;
        if (To == state) return false; // never restart the current state by transitioning to itself
        if (RequiresFinished && !finished) return false;

        foreach (var condition in Conditions)
            if (!condition.Evaluate(parameters)) return false;

        return true;
    }
}
