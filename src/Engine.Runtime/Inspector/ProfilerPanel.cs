using System.Numerics;
using Engine.Core.Diagnostics;
using ImGuiNET;

namespace Engine.Runtime.Inspector;

/// <summary>
/// The "Profiler" debug window: a table of every measured section (systems and game hooks) with last, average and worst frame
/// time, and a rolling graph of the whole CPU frame. Read it top-down to find what dominates the frame.
/// </summary>
internal sealed class ProfilerPanel(Profiler profiler)
{
    private const string FrameSection = "CPU frame";

    /// <summary>Draws the window. Call between ImGui frame begin and render.</summary>
    public void Draw()
    {
        ImGui.SetNextWindowPos(new Vector2(580f, 10f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(420f, 360f), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Profiler"))
        {
            ImGui.End();
            return;
        }

        DrawFrameGraph();
        DrawTable();
        ImGui.End();
    }

    private void DrawFrameGraph()
    {
        int frame = profiler.GetSection(FrameSection);
        var history = profiler.History(frame, out int oldest);
        float max = Math.Max(profiler.MaxMs(frame), 1f);
        string overlay = $"{profiler.LastMs(frame):F2} ms (avg {profiler.AverageMs(frame):F2}, max {profiler.MaxMs(frame):F2})";
        ImGui.PlotLines("##frame", ref history[0], history.Length, oldest, overlay, 0f, max * 1.2f, new Vector2(-1f, 70f));
    }

    private void DrawTable()
    {
        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp;
        if (!ImGui.BeginTable("sections", 4, flags)) return;

        ImGui.TableSetupColumn("Section");
        ImGui.TableSetupColumn("Last ms");
        ImGui.TableSetupColumn("Avg ms");
        ImGui.TableSetupColumn("Max ms");
        ImGui.TableHeadersRow();

        for (int i = 0; i < profiler.SectionCount; i++)
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(profiler.NameOf(i));
            ImGui.TableSetColumnIndex(1);
            ImGui.Text($"{profiler.LastMs(i):F3}");
            ImGui.TableSetColumnIndex(2);
            ImGui.Text($"{profiler.AverageMs(i):F3}");
            ImGui.TableSetColumnIndex(3);
            ImGui.Text($"{profiler.MaxMs(i):F3}");
        }

        ImGui.EndTable();
    }
}
