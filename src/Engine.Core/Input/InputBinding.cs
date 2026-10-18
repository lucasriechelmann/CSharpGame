namespace Engine.Core.Input;

/// <summary>One physical key or mouse button, with helpers to query it from an <see cref="InputState"/>.</summary>
public readonly struct InputBinding
{
    private readonly InputBindingKind _kind;
    private readonly int _code;

    private InputBinding(InputBindingKind kind, int code)
    {
        _kind = kind;
        _code = code;
    }

    /// <summary>Binds a keyboard key.</summary>
    public static InputBinding From(KeyCode key) => new(InputBindingKind.Key, (int)key);

    /// <summary>Binds a mouse button.</summary>
    public static InputBinding From(MouseButton button) => new(InputBindingKind.MouseButton, (int)button);

    /// <summary>True while the bound input is held.</summary>
    public bool IsDown(InputState input) => _kind == InputBindingKind.Key
        ? input.IsDown((KeyCode)_code)
        : input.IsDown((MouseButton)_code);

    /// <summary>True during the frame the bound input went down.</summary>
    public bool WasPressed(InputState input) => _kind == InputBindingKind.Key
        ? input.WasPressed((KeyCode)_code)
        : input.WasPressed((MouseButton)_code);

    /// <summary>True during the frame the bound input was released.</summary>
    public bool WasReleased(InputState input) => _kind == InputBindingKind.Key
        ? input.WasReleased((KeyCode)_code)
        : input.WasReleased((MouseButton)_code);

    /// <inheritdoc />
    public override string ToString() => _kind == InputBindingKind.Key ? ((KeyCode)_code).ToString() : $"Mouse{(MouseButton)_code}";
}
