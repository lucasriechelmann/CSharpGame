using System.Numerics;
using ImGuiNET;

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
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize;
        ImGui.SetNextWindowPos(new Vector2(Window.Width / 2f, Window.Height / 2f), ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        ImGui.Begin("##pause", flags);
        ImGui.SetWindowFontScale(3f);
        ImGui.TextColored(new Vector4(1f, 0.85f, 0.3f, 1f), "PAUSED");
        ImGui.SetWindowFontScale(1.8f);

        PauseChoice? clicked = null;
        foreach (var choice in _pauseMenu.Choices)
        {
            bool selected = choice == _pauseMenu.Selected;
            if (ImGui.Selectable(PauseMenu.Label(choice), selected, ImGuiSelectableFlags.None, new Vector2(260f, 0f))) clicked = choice;
            if (ImGui.IsItemHovered()) _pauseMenu.Select(choice);
        }

        ImGui.SetWindowFontScale(1f);
        ImGui.TextDisabled("Up/Down + Enter, or click.  Esc / P resumes.");
        ImGui.End();

        if (clicked is { } picked) Choose(picked);
    }
}
