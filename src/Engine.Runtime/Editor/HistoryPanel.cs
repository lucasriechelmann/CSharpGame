using System.Numerics;
using Engine.Core.Ecs;
using Engine.Runtime.Editor.Undo;
using ImGuiNET;

namespace Engine.Runtime.Editor;

/// <summary>
/// The editor's "History" window: every undoable step as a list, oldest at the top, with the steps that were undone greyed out below the
/// present. Clicking a step goes back or forward to just after it, so a mistake ten steps ago can be undone in one click.
/// </summary>
internal sealed class HistoryPanel(CommandStack history, Func<bool> canEdit, Action<Entity> select)
{
    /// <summary>Draws the window. Call between ImGui frame begin and render.</summary>
    public void Draw()
    {
        ImGui.SetNextWindowPos(new Vector2(260f, 300f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(260f, 240f), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("History"))
        {
            ImGui.End();
            return;
        }

        ImGui.BeginDisabled(!canEdit()); // while the game plays the world is restored on Stop, so there is no history to walk
        var undo = history.UndoCommands;
        int undoCount = undo.Count;
        var redo = history.RedoCommands.ToList();

        if (undoCount == 0 && redo.Count == 0) ImGui.TextDisabled("No edits yet.");
        for (int i = 0; i < undoCount; i++)
        {
            bool current = i == undoCount - 1;
            if (ImGui.Selectable($"{i + 1}. {undo[i].Description}##u{i}", current) && !current)
                Go(history.UndoMany(undoCount - 1 - i));
        }

        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
        for (int i = 0; i < redo.Count; i++)
        {
            if (ImGui.Selectable($"{undoCount + i + 1}. {redo[i].Description}##r{i}")) Go(history.RedoMany(i + 1));
        }

        ImGui.PopStyleColor();
        ImGui.EndDisabled();
        ImGui.End();
    }

    private void Go(IEditCommand? command)
    {
        if (command != null) select(command.Affected(history.Context));
    }
}
