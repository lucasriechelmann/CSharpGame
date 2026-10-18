using System.Numerics;
using Engine.Runtime.Menus;

namespace Arena3D;

// Pause: Escape or P freezes the simulation (Game.IsPaused), frees the mouse and opens the shared pause menu (Engine.Runtime.Menus).
public sealed partial class Arena3DGame
{
    private readonly PauseMenu _pauseMenu = new();

    /// <summary>Opens or closes the menu; only while a run is on (after it ends, Enter restarts).</summary>
    private void TogglePause()
    {
        if (!IsPaused && _session.Phase != OrbPhase.Playing) return;

        IsPaused = !IsPaused;
        _pauseMenu.Reset();
        _hasMouse = false; // the camera must not jump by whatever the mouse did while the menu was open
        Log.Info("Game", IsPaused ? "Paused" : "Resumed");
    }

    /// <summary>Menu keys while paused (the systems are frozen, only <c>Update</c> runs).</summary>
    private void UpdatePauseMenu()
    {
        if (Actions.WasPressed(GameActions.MenuUp)) _pauseMenu.Move(-1);
        if (Actions.WasPressed(GameActions.MenuDown)) _pauseMenu.Move(1);
        if (Actions.WasPressed(GameActions.MenuConfirm)) Choose(_pauseMenu.Selected);
    }

    private void Choose(PauseChoice choice)
    {
        switch (choice)
        {
            case PauseChoice.Resume:
                TogglePause();
                break;
            case PauseChoice.Restart:
                IsPaused = false;
                _hasMouse = false;
                Restart();
                break;
            case PauseChoice.Quit:
                Quit();
                break;
        }
    }

    private void DrawPauseMenu()
    {
        var clicked = PauseMenuView.Draw(_pauseMenu, ViewportOrigin, new Vector2(ViewportWidth, ViewportHeight), "Up/Down + Enter, or click.  Esc / P resumes.");
        if (clicked is { } picked) Choose(picked);
    }
}
