using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Ecs;
using Engine.Platform;
using ImGuiNET;

namespace Engine.Runtime;

/// <summary>The built-in "Engine" debug panel: timing, fixed-step info, entity count and window settings.</summary>
internal sealed class DebugOverlay(GameWindow window, FixedTimestep timestep, FrameStats stats, World world, AssetManager assets)
{
    /// <summary>Whether the overlay (and the game's own debug panels) are drawn. Toggled with F1.</summary>
    public bool Visible { get; private set; } = true;

    /// <summary>Flips <see cref="Visible"/>.</summary>
    public void Toggle() => Visible = !Visible;

    /// <summary>Draws the panel. Call between ImGui frame begin and render.</summary>
    public void Draw()
    {
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10f, 10f), ImGuiCond.FirstUseEver);
        ImGui.Begin("Engine");
        ImGui.Text($"FPS: {stats.Fps:F0}  ({stats.FrameMs:F2} ms)");
        ImGui.Text($"Fixed steps: {stats.FixedSteps}  (step {timestep.Step * 1000:F2} ms)");
        ImGui.Text($"Alpha: {timestep.Alpha:F2}");
        ImGui.Text($"Entities: {world.EntityCount}   Assets: {assets.Count}");
        ImGui.Text($"Window: {window.Width}x{window.Height}");

        bool vsync = window.VSync;
        if (ImGui.Checkbox("VSync", ref vsync)) window.VSync = vsync;

        ImGui.Text("F1: toggle debug UI");
        ImGui.End();
    }
}
