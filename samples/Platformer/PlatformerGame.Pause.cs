using System.Numerics;
using Engine.Runtime.Menus;

namespace Platformer;

// Pause: Escape or P freezes the simulation (Game.IsPaused) and opens a small menu drawn from Hud().
public sealed partial class PlatformerGame
{
    private readonly PauseMenu _pauseMenu = new();

    /// <summary>Opens or closes the menu. Only while playing: after the run ends Enter already restarts it.</summary>
    private void TogglePause()
    {
        if (!IsPaused && _session.Phase != GamePhase.Playing) return;

        IsPaused = !IsPaused;
        _pauseMenu.Reset();
        Log.Info("Game", IsPaused ? "Paused" : "Resumed");
    }

    /// <summary>Per-frame menu input while paused (the frame and fixed systems are frozen, so only this runs).</summary>
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
                RestartRun();
                break;
            case PauseChoice.Quit:
                Log.Info("Game", "Quit from the pause menu");
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
