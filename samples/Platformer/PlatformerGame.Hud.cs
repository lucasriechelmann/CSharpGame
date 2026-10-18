using System.Numerics;
using ImGuiNET;

namespace Platformer;

// The player-facing UI: a status line along the top and a big message when the run ends. Drawn even when F1 hides the debug UI.
public sealed partial class PlatformerGame
{
    private const ImGuiWindowFlags HudFlags =
        ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs |
        ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize;

    private bool _doorOpen;

    /// <inheritdoc />
    protected override void Hud()
    {
        DrawStatusLine();
        if (_session.Phase != GamePhase.Playing) DrawEndMessage();
    }

    private void DrawStatusLine()
    {
        ImGui.SetNextWindowPos(new Vector2(Window.Width / 2f, 12f), ImGuiCond.Always, new Vector2(0.5f, 0f));
        ImGui.Begin("##hud", HudFlags);
        ImGui.SetWindowFontScale(1.6f);

        var time = TimeSpan.FromSeconds(_session.Elapsed);
        ImGui.TextColored(new Vector4(1f, 0.85f, 0.3f, 1f), $"Coins {_session.CoinsCollected}/{_session.CoinsTotal}");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(1f, 0.45f, 0.45f, 1f), $"   Lives {_session.Lives}");
        ImGui.SameLine();
        ImGui.Text($"   Score {_session.Score}   {time:mm\\:ss}");

        if (_session.Phase == GamePhase.Playing)
        {
            ImGui.SetWindowFontScale(1.1f);
            ImGui.TextColored(
                _session.GoalUnlocked ? new Vector4(0.4f, 1f, 1f, 1f) : new Vector4(0.8f, 0.8f, 0.8f, 1f),
                _session.GoalUnlocked ? "The exit is open - reach the door!" : "Collect every coin to open the exit. Stomp creatures from above.");
        }

        ImGui.End();
    }

    private void DrawEndMessage()
    {
        bool won = _session.Phase == GamePhase.Won;

        ImGui.SetNextWindowPos(new Vector2(Window.Width / 2f, Window.Height / 2f - 150f), ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        ImGui.Begin("##endmessage", HudFlags);
        ImGui.SetWindowFontScale(3.5f);
        ImGui.TextColored(won ? new Vector4(0.4f, 1f, 0.5f, 1f) : new Vector4(1f, 0.4f, 0.4f, 1f), won ? "YOU WIN!" : "GAME OVER");
        ImGui.SetWindowFontScale(1.6f);
        ImGui.Text($"Score {_session.Score}   Time {TimeSpan.FromSeconds(_session.Elapsed):mm\\:ss}");
        ImGui.Text("Press Enter to play again");
        ImGui.End();
    }
}
