namespace Engine.Animation;

/// <summary>
/// A data-driven animation graph: named states that each play a clip, float parameters, and prioritised transitions
/// between states. Game code writes parameters (Speed, Grounded) and the graph picks the state, so gameplay never
/// says "play Run" directly. It is shared by every entity that uses it; per-entity playback lives in <see cref="SpriteAnimator"/>.
/// </summary>
public sealed class AnimationStateMachine
{
    private readonly List<string> _parameterNames = new();
    private readonly List<string> _stateNames = new();
    private readonly List<AnimationClip> _clips = new();
    private readonly List<AnimationTransition> _transitions = new();

    /// <summary>Index of the state new animators start in.</summary>
    public int DefaultState { get; set; }

    /// <summary>The parameter names in index order.</summary>
    public IReadOnlyList<string> ParameterNames => _parameterNames;

    /// <summary>The transitions in priority order (used to write the machine back to a file).</summary>
    public IReadOnlyList<AnimationTransition> Transitions => _transitions;

    /// <summary>Number of parameters; an animator's parameter array must be at least this long.</summary>
    public int ParameterCount => _parameterNames.Count;

    /// <summary>Number of states.</summary>
    public int StateCount => _stateNames.Count;

    /// <summary>Declares a float parameter and returns its index. Booleans use 0 and 1.</summary>
    /// <exception cref="ArgumentException">A parameter with that name already exists.</exception>
    public int AddParameter(string name)
    {
        if (_parameterNames.Contains(name)) throw new ArgumentException($"Parameter '{name}' already exists.", nameof(name));
        _parameterNames.Add(name);
        return _parameterNames.Count - 1;
    }

    /// <summary>Adds a state that plays <paramref name="clip"/> and returns its index. The first state added is the default.</summary>
    /// <exception cref="ArgumentException">A state with that name already exists.</exception>
    public int AddState(string name, AnimationClip clip)
    {
        if (_stateNames.Contains(name)) throw new ArgumentException($"State '{name}' already exists.", nameof(name));
        _stateNames.Add(name);
        _clips.Add(clip);
        return _stateNames.Count - 1;
    }

    /// <summary>
    /// Adds a transition. Transitions are tried in the order they were added, so add the more important ones first.
    /// </summary>
    /// <param name="from">Source state name, or null for "any state".</param>
    /// <param name="to">Target state name.</param>
    /// <param name="conditions">Tests that must all hold; null or empty means always.</param>
    /// <param name="requiresFinished">Wait for a <see cref="AnimationLoopMode.Once"/> clip to finish first.</param>
    /// <returns>This machine, so calls can be chained.</returns>
    public AnimationStateMachine AddTransition(string? from, string to, AnimationCondition[]? conditions = null, bool requiresFinished = false)
    {
        int fromIndex = from is null ? AnimationTransition.AnyState : GetStateIndex(from);
        int toIndex = GetStateIndex(to);

        conditions ??= [];
        foreach (var condition in conditions)
        {
            if (condition.Parameter < 0 || condition.Parameter >= ParameterCount)
                throw new ArgumentException($"Condition refers to parameter {condition.Parameter}, but only {ParameterCount} are declared.", nameof(conditions));
        }

        _transitions.Add(new AnimationTransition(fromIndex, toIndex, requiresFinished, conditions));
        return this;
    }

    /// <summary>Builds a condition by parameter name, for readable setup code.</summary>
    /// <exception cref="KeyNotFoundException">No parameter has that name.</exception>
    public AnimationCondition Condition(string parameter, AnimationComparison comparison, float value) =>
        new(GetParameterIndex(parameter), comparison, value);

    /// <summary>The index of a parameter, to cache and use when writing values every frame.</summary>
    /// <exception cref="KeyNotFoundException">No parameter has that name.</exception>
    public int GetParameterIndex(string name)
    {
        int index = _parameterNames.IndexOf(name);
        return index >= 0 ? index : throw new KeyNotFoundException($"No animation parameter named '{name}'.");
    }

    /// <summary>The index of a state.</summary>
    /// <exception cref="KeyNotFoundException">No state has that name.</exception>
    public int GetStateIndex(string name)
    {
        int index = _stateNames.IndexOf(name);
        return index >= 0 ? index : throw new KeyNotFoundException($"No animation state named '{name}'.");
    }

    /// <summary>The name of a state, for debug displays.</summary>
    public string GetStateName(int state) => _stateNames[state];

    /// <summary>The clip a state plays.</summary>
    public AnimationClip GetClip(int state) => _clips[state];

    /// <summary>
    /// Finds the first transition (in priority order) that applies in <paramref name="state"/> with the current parameters.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="parameters">Current parameter values.</param>
    /// <param name="finished">Whether the current clip has finished.</param>
    /// <returns>The target state index, or -1 when no transition fires.</returns>
    public int FindTransition(int state, ReadOnlySpan<float> parameters, bool finished)
    {
        foreach (var transition in _transitions)
            if (transition.IsSatisfied(state, parameters, finished)) return transition.To;

        return -1;
    }
}
