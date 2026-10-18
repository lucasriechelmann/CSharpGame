namespace Engine.Core.Input;

/// <summary>
/// Maps named actions ("Jump", "Fire") and axes ("MoveX") to physical keys and mouse buttons, so game code
/// asks for intent rather than for a specific key. An action can have several bindings (e.g. Space and a mouse
/// button), and rebinding never touches game logic.
/// </summary>
public sealed class ActionMap(InputState input)
{
    private readonly Dictionary<string, List<InputBinding>> _actions = new();
    private readonly Dictionary<string, List<AxisBinding>> _axes = new();

    /// <summary>Adds a key to an action. The action is created on first use; call again to add alternatives.</summary>
    public ActionMap Bind(string action, KeyCode key) => AddAction(action, InputBinding.From(key));

    /// <summary>Adds a mouse button to an action.</summary>
    public ActionMap Bind(string action, MouseButton button) => AddAction(action, InputBinding.From(button));

    /// <summary>Adds a negative/positive key pair to an axis. Call again to add alternatives (e.g. arrow keys as well as A/D).</summary>
    public ActionMap BindAxis(string axis, KeyCode negative, KeyCode positive)
    {
        if (!_axes.TryGetValue(axis, out var list))
            _axes[axis] = list = new List<AxisBinding>();
        list.Add(new AxisBinding(InputBinding.From(negative), InputBinding.From(positive)));
        return this;
    }

    /// <summary>True while any binding of the action is held.</summary>
    /// <exception cref="KeyNotFoundException">The action was never bound (usually a typo).</exception>
    public bool IsDown(string action)
    {
        foreach (var binding in Get(action))
            if (binding.IsDown(input)) return true;
        return false;
    }

    /// <summary>True during the frame any binding of the action went down.</summary>
    /// <exception cref="KeyNotFoundException">The action was never bound.</exception>
    public bool WasPressed(string action)
    {
        foreach (var binding in Get(action))
            if (binding.WasPressed(input)) return true;
        return false;
    }

    /// <summary>True during the frame any binding of the action was released.</summary>
    /// <exception cref="KeyNotFoundException">The action was never bound.</exception>
    public bool WasReleased(string action)
    {
        foreach (var binding in Get(action))
            if (binding.WasReleased(input)) return true;
        return false;
    }

    /// <summary>The axis value in -1..1: the sum of every binding pair, clamped, so two bindings held together do not exceed 1.</summary>
    /// <exception cref="KeyNotFoundException">The axis was never bound.</exception>
    public float GetAxis(string axis)
    {
        if (!_axes.TryGetValue(axis, out var list))
            throw new KeyNotFoundException($"No axis named '{axis}' is bound.");

        float sum = 0f;
        foreach (var binding in list) sum += binding.Evaluate(input);
        return Math.Clamp(sum, -1f, 1f);
    }

    private ActionMap AddAction(string action, InputBinding binding)
    {
        if (!_actions.TryGetValue(action, out var list))
            _actions[action] = list = new List<InputBinding>();
        list.Add(binding);
        return this;
    }

    private List<InputBinding> Get(string action) =>
        _actions.TryGetValue(action, out var list)
            ? list
            : throw new KeyNotFoundException($"No action named '{action}' is bound.");
}
