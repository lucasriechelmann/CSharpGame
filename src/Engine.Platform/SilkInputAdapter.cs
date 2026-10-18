using System.Numerics;
using Engine.Core.Input;
using Silk.NET.Input;
using EngineMouseButton = Engine.Core.Input.MouseButton;
using SilkMouseButton = Silk.NET.Input.MouseButton;

namespace Engine.Platform;

/// <summary>
/// Listens to Silk.NET keyboard and mouse events and writes them into an <see cref="InputState"/>. Button/key
/// presses are dropped while a UI layer says it wants the input (so typing in a text box does not also move the
/// player), but releases always go through so nothing gets stuck down.
/// </summary>
internal sealed class SilkInputAdapter
{
    private readonly InputState _state;
    private readonly Func<bool> _uiWantsKeyboard;
    private readonly Func<bool> _uiWantsMouse;

    /// <summary>Subscribes to every keyboard and mouse in the context.</summary>
    /// <param name="context">Silk input context to listen to.</param>
    /// <param name="state">Where input is written.</param>
    /// <param name="uiWantsKeyboard">Returns true while a UI widget is capturing the keyboard.</param>
    /// <param name="uiWantsMouse">Returns true while the mouse is over or captured by a UI widget.</param>
    public SilkInputAdapter(IInputContext context, InputState state, Func<bool> uiWantsKeyboard, Func<bool> uiWantsMouse)
    {
        _state = state;
        _uiWantsKeyboard = uiWantsKeyboard;
        _uiWantsMouse = uiWantsMouse;

        foreach (var keyboard in context.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
        }

        foreach (var mouse in context.Mice)
        {
            mouse.MouseDown += OnMouseDown;
            mouse.MouseUp += OnMouseUp;
            mouse.MouseMove += OnMouseMove;
            mouse.Scroll += OnScroll;
        }
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        if (_uiWantsKeyboard()) return;
        if (SilkKeyMap.TryGet(key, out var code)) _state.SetKey(code, true);
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
    {
        if (SilkKeyMap.TryGet(key, out var code)) _state.SetKey(code, false);
    }

    private void OnMouseDown(IMouse mouse, SilkMouseButton button)
    {
        if (_uiWantsMouse()) return;
        if (TryTranslate(button, out var translated)) _state.SetMouseButton(translated, true);
    }

    private void OnMouseUp(IMouse mouse, SilkMouseButton button)
    {
        if (TryTranslate(button, out var translated)) _state.SetMouseButton(translated, false);
    }

    private void OnMouseMove(IMouse mouse, Vector2 position) => _state.SetMousePosition(position);

    private void OnScroll(IMouse mouse, ScrollWheel wheel)
    {
        if (!_uiWantsMouse()) _state.AddScroll(wheel.Y);
    }

    private static bool TryTranslate(SilkMouseButton button, out EngineMouseButton translated)
    {
        switch (button)
        {
            case SilkMouseButton.Left: translated = EngineMouseButton.Left; return true;
            case SilkMouseButton.Right: translated = EngineMouseButton.Right; return true;
            case SilkMouseButton.Middle: translated = EngineMouseButton.Middle; return true;
            default: translated = default; return false;
        }
    }
}
