namespace Engine.Core.Input;

/// <summary>
/// Physical keyboard keys the engine understands. Member names intentionally match Silk.NET's key names
/// so the platform layer can translate by name; keys not listed here are ignored.
/// </summary>
public enum KeyCode
{
    /// <summary>Placeholder for "no key"; never reported as pressed.</summary>
    None = 0,

    /// <summary>Space bar.</summary>
    Space,

    /// <summary>Digit row 0.</summary>
    Number0,

    /// <summary>Digit row 1.</summary>
    Number1,

    /// <summary>Digit row 2.</summary>
    Number2,

    /// <summary>Digit row 3.</summary>
    Number3,

    /// <summary>Digit row 4.</summary>
    Number4,

    /// <summary>Digit row 5.</summary>
    Number5,

    /// <summary>Digit row 6.</summary>
    Number6,

    /// <summary>Digit row 7.</summary>
    Number7,

    /// <summary>Digit row 8.</summary>
    Number8,

    /// <summary>Digit row 9.</summary>
    Number9,

    /// <summary>Letter A.</summary>
    A,

    /// <summary>Letter B.</summary>
    B,

    /// <summary>Letter C.</summary>
    C,

    /// <summary>Letter D.</summary>
    D,

    /// <summary>Letter E.</summary>
    E,

    /// <summary>Letter F.</summary>
    F,

    /// <summary>Letter G.</summary>
    G,

    /// <summary>Letter H.</summary>
    H,

    /// <summary>Letter I.</summary>
    I,

    /// <summary>Letter J.</summary>
    J,

    /// <summary>Letter K.</summary>
    K,

    /// <summary>Letter L.</summary>
    L,

    /// <summary>Letter M.</summary>
    M,

    /// <summary>Letter N.</summary>
    N,

    /// <summary>Letter O.</summary>
    O,

    /// <summary>Letter P.</summary>
    P,

    /// <summary>Letter Q.</summary>
    Q,

    /// <summary>Letter R.</summary>
    R,

    /// <summary>Letter S.</summary>
    S,

    /// <summary>Letter T.</summary>
    T,

    /// <summary>Letter U.</summary>
    U,

    /// <summary>Letter V.</summary>
    V,

    /// <summary>Letter W.</summary>
    W,

    /// <summary>Letter X.</summary>
    X,

    /// <summary>Letter Y.</summary>
    Y,

    /// <summary>Letter Z.</summary>
    Z,

    /// <summary>Escape.</summary>
    Escape,

    /// <summary>Enter / Return.</summary>
    Enter,

    /// <summary>Tab.</summary>
    Tab,

    /// <summary>Backspace.</summary>
    Backspace,

    /// <summary>Arrow right.</summary>
    Right,

    /// <summary>Arrow left.</summary>
    Left,

    /// <summary>Arrow down.</summary>
    Down,

    /// <summary>Arrow up.</summary>
    Up,

    /// <summary>Function key F1.</summary>
    F1,

    /// <summary>Function key F2.</summary>
    F2,

    /// <summary>Function key F3.</summary>
    F3,

    /// <summary>Function key F4.</summary>
    F4,

    /// <summary>Function key F5.</summary>
    F5,

    /// <summary>Function key F6.</summary>
    F6,

    /// <summary>Function key F7.</summary>
    F7,

    /// <summary>Function key F8.</summary>
    F8,

    /// <summary>Function key F9.</summary>
    F9,

    /// <summary>Function key F10.</summary>
    F10,

    /// <summary>Function key F11.</summary>
    F11,

    /// <summary>Function key F12.</summary>
    F12,

    /// <summary>Left Shift.</summary>
    ShiftLeft,

    /// <summary>Right Shift.</summary>
    ShiftRight,

    /// <summary>Left Control.</summary>
    ControlLeft,

    /// <summary>Right Control.</summary>
    ControlRight,

    /// <summary>Left Alt.</summary>
    AltLeft,

    /// <summary>Right Alt.</summary>
    AltRight,

    /// <summary>Minus / underscore key.</summary>
    Minus,

    /// <summary>Equals / plus key.</summary>
    Equal,

    /// <summary>Comma key.</summary>
    Comma,

    /// <summary>Period key.</summary>
    Period,
}
