using System.Numerics;
using Engine.Core.Ecs;
using Engine.Scenes;
using ImGuiNET;

namespace Engine.Runtime.Inspector;

// Right pane: the selected entity's components, with add / remove.
internal sealed partial class EntityInspector
{
    private void DrawDetails()
    {
        ImGui.BeginChild("details", Vector2.Zero);

        var entity = Selected;
        if (entity.IsNull)
        {
            ImGui.TextDisabled("Select an entity in the list or by clicking it in the scene.");
            ImGui.EndChild();
            return;
        }

        ImGui.Text(entity.ToString());

        _componentTypes.Clear();
        world.GetComponentTypes(entity, _componentTypes);

        Type? toRemove = null;
        foreach (var type in _componentTypes)
        {
            ImGui.PushID(type.Name);
            bool open = ImGui.CollapsingHeader(type.Name, ImGuiTreeNodeFlags.DefaultOpen);
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - 8f);
            if (ImGui.SmallButton("x")) toRemove = type;

            if (open) DrawComponent(entity, type);
            ImGui.PopID();
        }

        if (toRemove != null) world.Remove(entity, toRemove);

        ImGui.Separator();
        DrawAddComponent(entity);
        ImGui.EndChild();
    }

    /// <summary>Copies the component out, lets every field be edited, and writes it back only if something changed.</summary>
    private void DrawComponent(Entity entity, Type type)
    {
        object? boxed = world.GetBoxed(entity, type);
        if (FieldEditor.DrawBody(type, ref boxed))
            world.SetBoxed(entity, type, boxed!);
    }

    /// <summary>Lists the registered components the entity lacks; picking one adds it with its registered starting value.</summary>
    private void DrawAddComponent(Entity entity)
    {
        if (!ImGui.BeginCombo("##add", "Add component...")) return;

        foreach (var info in registry.Describe())
        {
            if (world.Has(entity, info.Type)) continue;
            if (ImGui.Selectable(info.Name)) world.AddBoxed(entity, info.Type, info.DefaultValue);
        }

        ImGui.EndCombo();
    }
}
