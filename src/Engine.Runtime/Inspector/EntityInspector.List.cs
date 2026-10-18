using System.Numerics;
using Engine.Core.Ecs;
using ImGuiNET;

namespace Engine.Runtime.Inspector;

// Left pane: filterable list of entities, plus create / destroy.
internal sealed partial class EntityInspector
{
    private void DrawEntityList()
    {
        ImGui.BeginChild("entities", new Vector2(190f, 0f), ImGuiChildFlags.Border);

        if (ImGui.Button("+ Entity"))
            Selected = world.CreateEntity();

        ImGui.SameLine();
        ImGui.BeginDisabled(Selected.IsNull);
        if (ImGui.Button("Destroy"))
        {
            world.DestroyEntity(Selected);
            Selected = Entity.Null;
        }

        ImGui.EndDisabled();

        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("##filter", "filter...", ref _filter, 64);
        ImGui.Separator();

        // Copy first: the list is drawn while the world could change (nothing here mutates it, but a click handler might).
        _entities.Clear();
        foreach (var entity in world.Entities) _entities.Add(entity);

        foreach (var entity in _entities)
        {
            string label = LabelOf(entity);
            if (_filter.Length > 0 && !label.Contains(_filter, StringComparison.OrdinalIgnoreCase)) continue;

            if (ImGui.Selectable($"{label}##{entity.Id}", entity == Selected))
                Selected = entity;
        }

        ImGui.EndChild();
    }
}
