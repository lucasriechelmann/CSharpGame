namespace Engine.Core.Input;

/// <summary>A pair of inputs that push an axis towards -1 and +1 (for example A and D for horizontal movement).</summary>
/// <param name="Negative">Input that drives the axis to -1 while held.</param>
/// <param name="Positive">Input that drives the axis to +1 while held.</param>
public readonly record struct AxisBinding(InputBinding Negative, InputBinding Positive)
{
    /// <summary>The axis value contributed by this pair: -1, 0 or +1 (0 when both or neither are held).</summary>
    public float Evaluate(InputState input)
    {
        float value = 0f;
        if (Negative.IsDown(input)) value -= 1f;
        if (Positive.IsDown(input)) value += 1f;
        return value;
    }
}
