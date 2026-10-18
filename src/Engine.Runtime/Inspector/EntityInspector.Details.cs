using System.Numerics;
using Engine.Core.Ecs;
using Engine.Runtime.Editor.Undo;
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

        if (toRemove != null) RemoveComponent(entity, toRemove);

        ImGui.Separator();
        DrawAddComponent(entity);
        ImGui.EndChild();
    }

    /// <summary>Copies the component out, lets every field be edited, and writes it back only if something changed.</summary>
    private void DrawComponent(Entity entity, Type type)
    {
        object? boxed = world.GetBoxed(entity, type);
        object before = world.GetBoxed(entity, type); // a separate copy: drawing edits the box above in place
        if (!FieldEditor.DrawBody(type, ref boxed)) return;

        world.SetBoxed(entity, type, boxed!);
        if (History != null && _pendingEdit is null) _pendingEdit = new PendingEdit(entity, type, before);
    }

    /// <summary>
    /// Turns a finished edit gesture into one undo step. A drag changes the value every frame, so the first change of a gesture
    /// remembers the value before it, and the step is recorded when nothing is being edited any more (mouse released, field
    /// left, checkbox clicked), or at once when another entity is selected.
    /// </summary>
    public void CommitPendingEdit()
    {
        if (_pendingEdit is not { } pending) return;
        if (ImGui.IsAnyItemActive() && pending.Entity == Selected) return; // still dragging or typing

        _pendingEdit = null;
        if (History == null || !world.IsAlive(pending.Entity) || !world.Has(pending.Entity, pending.Type)) return;

        object after = world.GetBoxed(pending.Entity, pending.Type);
        if (Equals(pending.Before, after)) return;

        History.Push(new SetComponentCommand(History.Context, pending.Entity, pending.Type, pending.Before, after, LabelOf(pending.Entity)));
    }

    private void RemoveComponent(Entity entity, Type type)
    {
        CommitPendingEdit();
        if (History != null) History.Execute(new RemoveComponentCommand(History.Context, entity, type, LabelOf(entity)));
        else world.Remove(entity, type);
    }

    private void AddComponent(Entity entity, ComponentInfo info)
    {
        CommitPendingEdit();
        if (History != null) History.Execute(new AddComponentCommand(History.Context, entity, info.Type, info.DefaultValue, LabelOf(entity)));
        else world.AddBoxed(entity, info.Type, info.DefaultValue);
    }

    /// <summary>The value a component had before the current edit gesture began.</summary>
    private readonly record struct PendingEdit(Entity Entity, Type Type, object Before);

    /// <summary>Lists the registered components the entity lacks; picking one adds it with its registered starting value.</summary>
    private void DrawAddComponent(Entity entity)
    {
        if (!ImGui.BeginCombo("##add", "Add component...")) return;

        foreach (var info in registry.Describe())
        {
            if (world.Has(entity, info.Type)) continue;
            if (ImGui.Selectable(info.Name)) AddComponent(entity, info);
        }

        ImGui.EndCombo();
    }
}
