using System.Numerics;
using Engine.Core.Ecs;
using Engine.Runtime.Inspector;
using ImGuiNET;

namespace Engine.Runtime.Editor;

/// <summary>
/// The editor's "Hierarchy" window: every entity in the world as a filterable list. Clicking selects (the selection is shared with
/// the inspector and the gizmo), and the buttons raise the editor's New, Duplicate and Delete commands. Entities have no parent
/// links yet, so the list is flat; a tree comes when transforms get parents.
/// </summary>
internal sealed class HierarchyPanel(World world, EntityInspector selection, Action newEntity, Action duplicate, Action delete)
{
    private readonly List<Entity> _entities = new();
    private string _filter = string.Empty;

    /// <summary>Draws the window. Call between ImGui frame begin and render.</summary>
    public void Draw()
    {
        ImGui.SetNextWindowPos(new Vector2(10f, 40f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(240f, 520f), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Hierarchy"))
        {
            ImGui.End();
            return;
        }

        if (ImGui.Button("+ New")) newEntity();
        ImGui.SameLine();
        ImGui.BeginDisabled(selection.Selected.IsNull);
        if (ImGui.Button("Duplicate")) duplicate();
        ImGui.SameLine();
        if (ImGui.Button("Delete")) delete();
        ImGui.EndDisabled();

        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("##filter", "filter...", ref _filter, 64);
        ImGui.TextDisabled($"{world.EntityCount} entities");
        ImGui.Separator();

        // Copy first: a click handler may change the world while the list is being drawn.
        _entities.Clear();
        foreach (var entity in world.Entities) _entities.Add(entity);

        ImGui.BeginChild("list");
        foreach (var entity in _entities)
        {
            string label = EntityLabel.Of(world, entity);
            if (_filter.Length > 0 && !label.Contains(_filter, StringComparison.OrdinalIgnoreCase)) continue;

            if (ImGui.Selectable($"{label}##{entity.Id}", entity == selection.Selected))
                selection.Selected = entity;
        }

        ImGui.EndChild();
        ImGui.End();
    }
}
