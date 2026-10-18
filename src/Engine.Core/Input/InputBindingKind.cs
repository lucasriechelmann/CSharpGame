namespace Engine.Core.Input;

/// <summary>Which kind of physical input an <see cref="InputBinding"/> refers to.</summary>
public enum InputBindingKind
{
    /// <summary>A keyboard key (<see cref="KeyCode"/>).</summary>
    Key,

    /// <summary>A mouse button (<see cref="MouseButton"/>).</summary>
    MouseButton,
}
