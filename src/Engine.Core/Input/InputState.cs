using System.Numerics;

namespace Engine.Core.Input;

/// <summary>
/// The current state of the keyboard and mouse, plus which keys/buttons went down or up since the last
/// <see cref="EndFrame"/>. It holds data only and knows nothing about the OS: the platform layer feeds it
/// through the <c>Set*</c>/<see cref="AddScroll"/> methods, and game code reads it (usually via <see cref="ActionMap"/>).
/// </summary>
/// <remarks>
/// "Pressed" and "released" are edge flags valid for one frame. Read them in per-frame code and latch the result
/// into a component; fixed-step systems can run several times per frame and would otherwise see the edge repeatedly.
/// </remarks>
public sealed class InputState
{
    private static readonly int KeyCount = Enum.GetValues<KeyCode>().Max(k => (int)k) + 1;
    private static readonly int ButtonCount = Enum.GetValues<MouseButton>().Max(b => (int)b) + 1;

    private readonly bool[] _keyDown = new bool[KeyCount];
    private readonly bool[] _keyPressed = new bool[KeyCount];
    private readonly bool[] _keyReleased = new bool[KeyCount];
    private readonly bool[] _buttonDown = new bool[ButtonCount];
    private readonly bool[] _buttonPressed = new bool[ButtonCount];
    private readonly bool[] _buttonReleased = new bool[ButtonCount];

    private Vector2 _rawMouse;
    private Vector2 _mouseOffset;

    /// <summary>Mouse position in window pixels (or relative to the game view when a host set <see cref="SetMouseOffset"/>), origin at the top-left, Y pointing down.</summary>
    public Vector2 MousePosition => _rawMouse - _mouseOffset;

    /// <summary>Wheel movement accumulated this frame (positive = scrolled up/away from the user).</summary>
    public float ScrollDelta { get; private set; }

    // ---- reading ----

    /// <summary>True while the key is held.</summary>
    public bool IsDown(KeyCode key) => _keyDown[(int)key];

    /// <summary>True only during the frame the key went down.</summary>
    public bool WasPressed(KeyCode key) => _keyPressed[(int)key];

    /// <summary>True only during the frame the key was let go.</summary>
    public bool WasReleased(KeyCode key) => _keyReleased[(int)key];

    /// <summary>True while the mouse button is held.</summary>
    public bool IsDown(MouseButton button) => _buttonDown[(int)button];

    /// <summary>True only during the frame the button went down.</summary>
    public bool WasPressed(MouseButton button) => _buttonPressed[(int)button];

    /// <summary>True only during the frame the button was let go.</summary>
    public bool WasReleased(MouseButton button) => _buttonReleased[(int)button];

    // ---- feeding (called by the platform layer) ----

    /// <summary>Records a key going down or up. Repeats while already down, and releases of keys that are up, are ignored.</summary>
    public void SetKey(KeyCode key, bool down)
    {
        if (key == KeyCode.None) return;
        Apply(_keyDown, _keyPressed, _keyReleased, (int)key, down);
    }

    /// <summary>Records a mouse button going down or up.</summary>
    public void SetMouseButton(MouseButton button, bool down) =>
        Apply(_buttonDown, _buttonPressed, _buttonReleased, (int)button, down);

    /// <summary>Records the latest mouse position in window pixels.</summary>
    public void SetMousePosition(Vector2 position) => _rawMouse = position;

    /// <summary>
    /// Makes <see cref="MousePosition"/> relative to a point in the window. A host that shows the game inside a panel (the editor) passes the panel's top-left corner,
    /// so game code reads the mouse in the game's own pixels without knowing it is in a panel.
    /// </summary>
    /// <param name="origin">Window position of the game view's top-left corner.</param>
    public void SetMouseOffset(Vector2 origin) => _mouseOffset = origin;

    /// <summary>Adds wheel movement for this frame.</summary>
    public void AddScroll(float amount) => ScrollDelta += amount;

    /// <summary>Marks everything as released (and raises release edges). Call when the window loses focus so keys cannot stay "stuck".</summary>
    public void ReleaseAll()
    {
        for (int i = 0; i < _keyDown.Length; i++) Apply(_keyDown, _keyPressed, _keyReleased, i, false);
        for (int i = 0; i < _buttonDown.Length; i++) Apply(_buttonDown, _buttonPressed, _buttonReleased, i, false);
    }

    /// <summary>Clears the one-frame edge flags and scroll. Call once per frame after all input has been read.</summary>
    public void EndFrame()
    {
        Array.Clear(_keyPressed);
        Array.Clear(_keyReleased);
        Array.Clear(_buttonPressed);
        Array.Clear(_buttonReleased);
        ScrollDelta = 0f;
    }

    private static void Apply(bool[] down, bool[] pressed, bool[] released, int index, bool isDown)
    {
        if (down[index] == isDown) return;

        down[index] = isDown;
        if (isDown) pressed[index] = true;
        else released[index] = true;
    }
}
