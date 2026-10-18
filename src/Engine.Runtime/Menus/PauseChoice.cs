namespace Engine.Runtime.Menus;

/// <summary>What a pause menu entry does when chosen.</summary>
public enum PauseChoice
{
    /// <summary>Close the menu and carry on.</summary>
    Resume,

    /// <summary>Abandon this run and start the level over.</summary>
    Restart,

    /// <summary>Close the game.</summary>
    Quit,
}
