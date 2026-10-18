namespace Engine.Runtime.Menus;

/// <summary>
/// The pause menu's state: which entry is highlighted and what choosing it means. Pure (no engine or UI types) so the navigation
/// rules are unit-tested; the game feeds it key presses and draws it in <c>Game.Hud()</c>.
/// </summary>
public sealed class PauseMenu
{
    private static readonly PauseChoice[] AllChoices = [PauseChoice.Resume, PauseChoice.Restart, PauseChoice.Quit];

    /// <summary>The entries in display order.</summary>
    public IReadOnlyList<PauseChoice> Choices => AllChoices;

    /// <summary>Index into <see cref="Choices"/> of the highlighted entry.</summary>
    public int SelectedIndex { get; private set; }

    /// <summary>The highlighted entry.</summary>
    public PauseChoice Selected => AllChoices[SelectedIndex];

    /// <summary>Moves the highlight by <paramref name="delta"/> entries, wrapping at both ends.</summary>
    /// <param name="delta">+1 for down, -1 for up.</param>
    public void Move(int delta) => SelectedIndex = ((SelectedIndex + delta) % AllChoices.Length + AllChoices.Length) % AllChoices.Length;

    /// <summary>Highlights <paramref name="choice"/> (a mouse hover or click).</summary>
    /// <param name="choice">The entry to highlight.</param>
    public void Select(PauseChoice choice) => SelectedIndex = Array.IndexOf(AllChoices, choice);

    /// <summary>Puts the highlight back on <see cref="PauseChoice.Resume"/>, so each time the menu opens, Enter means "carry on".</summary>
    public void Reset() => SelectedIndex = 0;

    /// <summary>The text shown for <paramref name="choice"/>.</summary>
    /// <param name="choice">The entry to name.</param>
    public static string Label(PauseChoice choice) => choice switch
    {
        PauseChoice.Resume => "Resume",
        PauseChoice.Restart => "Restart run",
        PauseChoice.Quit => "Quit",
        _ => throw new ArgumentOutOfRangeException(nameof(choice), choice, "Unknown pause choice."),
    };
}
