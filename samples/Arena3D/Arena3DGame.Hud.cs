using System.Numerics;
using Engine.Runtime.Menus;
using ImGuiNET;

namespace Arena3D;

// The player-facing UI: a status line along the top and a message when the run ends. Drawn even when the debug UI is hidden.
public sealed partial class Arena3DGame
{
    private const ImGuiWindowFlags HudFlags =
        ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs |
        ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize;

    /// <inheritdoc />
    protected override void Hud()
    {
        ImGui.SetNextWindowPos(ViewportOrigin + new Vector2(ViewportWidth / 2f, 12f), ImGuiCond.Always, new Vector2(0.5f, 0f));
        ImGui.SetNextWindowBgAlpha(0.5f); // the sky behind the bar is bright
        ImGui.Begin("##hud", HudFlags & ~ImGuiWindowFlags.NoBackground);
        ImGui.SetWindowFontScale(1.6f);
        ImGui.TextColored(new Vector4(1f, 0.85f, 0.3f, 1f), $"Orbs {_session.OrbsCollected}/{_session.OrbsTotal}");
        ImGui.SameLine();
        ImGui.TextColored(
            _session.TimeLeft < 15.0 ? new Vector4(1f, 0.4f, 0.4f, 1f) : new Vector4(1f, 1f, 1f, 1f),
            $"   Time {Math.Ceiling(_session.TimeLeft):0}");
        ImGui.SameLine();
        ImGui.Text($"   Score {_session.Score}");
        if (_session.Phase == OrbPhase.Playing)
        {
            ImGui.SetWindowFontScale(1.0f);
            ImGui.TextColored(new Vector4(0.85f, 0.85f, 0.85f, 1f), "W/A/S/D walk, Space jump, mouse turns the camera, Esc pauses. Falling off costs 5 seconds.");
        }

        ImGui.End();

        if (IsPaused) DrawPauseMenu();
        else if (_session.Phase != OrbPhase.Playing) DrawEndMessage();
    }

    private void DrawEndMessage()
    {
        ImGui.SetNextWindowPos(ViewportOrigin + new Vector2(ViewportWidth / 2f, ViewportHeight / 2f - 60f), ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        ImGui.Begin("##end", HudFlags);
        bool won = _session.Phase == OrbPhase.Won;
        ImGui.SetWindowFontScale(3f);
        ImGui.TextColored(won ? new Vector4(0.4f, 1f, 0.6f, 1f) : new Vector4(1f, 0.4f, 0.4f, 1f), won ? "YOU WIN!" : "TIME'S UP");
        ImGui.SetWindowFontScale(1.5f);
        ImGui.Text($"Orbs {_session.OrbsCollected}/{_session.OrbsTotal}   Score {_session.Score}   Falls {_session.Falls}");
        ImGui.Text("Press Enter to play again");
        ImGui.End();
    }

    /// <inheritdoc />
    protected override void DebugGui()
    {
        ImGui.SetNextWindowPos(new Vector2(12f, 120f), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Arena"))
        {
            ImGui.Text($"Meshes drawn: {_meshes.DrawnLastFrame} in {_meshes.DrawCallsLastFrame} draw calls  culled: {_meshes.CulledLastFrame}");
            ImGui.Text($"Phase {_session.Phase}   time left {_session.TimeLeft:F1}   falls {_session.Falls}");
            float distance = _zoom;
            if (ImGui.SliderFloat("Camera distance", ref distance, MinZoom, MaxZoom)) _zoom = distance;
            bool sky = _meshes.Lighting.DrawSky;
            if (ImGui.Checkbox("Procedural sky", ref sky)) _meshes.Lighting.DrawSky = sky;
            bool shadows = _meshes.Lighting.ShadowsEnabled;
            if (ImGui.Checkbox("Sun shadows", ref shadows)) _meshes.Lighting.ShadowsEnabled = shadows;
        }

        ImGui.End();
    }
}
