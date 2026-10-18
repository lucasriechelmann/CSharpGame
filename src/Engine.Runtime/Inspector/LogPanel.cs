using System.Numerics;
using Engine.Core.Logging;
using ImGuiNET;

namespace Engine.Runtime.Inspector;

/// <summary>
/// The "Log" debug window: the recent entries held by a <see cref="MemoryLogSink"/>, coloured by level, with a minimum-level
/// filter and a Clear button. It scrolls to the newest line while you are at the bottom, so reading older lines is not interrupted.
/// </summary>
internal sealed class LogPanel(MemoryLogSink sink, Logger logger)
{
    private static readonly string[] LevelNames = Enum.GetNames<LogLevel>();

    private long _lastSeenTotal;

    /// <summary>Draws the window. Call between ImGui frame begin and render.</summary>
    public void Draw()
    {
        ImGui.SetNextWindowPos(new Vector2(280f, 380f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(520f, 200f), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Log"))
        {
            ImGui.End();
            return;
        }

        int level = (int)logger.MinimumLevel;
        ImGui.SetNextItemWidth(110f);
        if (ImGui.Combo("Minimum level", ref level, LevelNames, LevelNames.Length)) logger.MinimumLevel = (LogLevel)level;
        ImGui.SameLine();
        if (ImGui.Button("Clear")) sink.Clear();
        ImGui.Separator();

        ImGui.BeginChild("lines", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);
        for (int i = 0; i < sink.Count; i++)
        {
            var entry = sink[i];
            ImGui.PushStyleColor(ImGuiCol.Text, ColorOf(entry.Level));
            ImGui.TextUnformatted(ConsoleLogSink.Format(entry));
            ImGui.PopStyleColor();
        }

        // Follow new lines only when already at the bottom, so scrolling up to read is not undone.
        bool grew = sink.TotalWritten != _lastSeenTotal;
        _lastSeenTotal = sink.TotalWritten;
        if (grew && ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 1f) ImGui.SetScrollHereY(1f);

        ImGui.EndChild();
        ImGui.End();
    }

    private static uint ColorOf(LogLevel level) => ImGui.ColorConvertFloat4ToU32(level switch
    {
        LogLevel.Debug => new Vector4(0.6f, 0.6f, 0.65f, 1f),
        LogLevel.Info => new Vector4(0.9f, 0.9f, 0.9f, 1f),
        LogLevel.Warning => new Vector4(1f, 0.8f, 0.3f, 1f),
        _ => new Vector4(1f, 0.4f, 0.4f, 1f),
    });
}
