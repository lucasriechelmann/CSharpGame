using System.Numerics;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;
using Engine.Runtime.Inspector;
using ImGuiNET;

namespace Engine.Runtime.Editor;

/// <summary>
/// The editor's "Hierarchy" window: the world as a tree of parents and children (a filter turns it into a flat list of matches).
/// Clicking selects (the selection is shared with the inspector and the gizmo), dragging an entity onto another makes it that
/// entity's child, dropping it on the "unparent" strip at the bottom makes it a root again, and the buttons raise the editor's New,
/// Child, Duplicate, Delete and Unparent commands.
/// </summary>
internal sealed class HierarchyPanel(
    World world,
    EntityInspector selection,
    Func<bool> canEdit,
    Action newEntity,
    Action newChild,
    Action duplicate,
    Action delete,
    Action<Entity, Entity> reparent)
{
    private const string DragPayload = "ENTITY";

    private readonly List<Entity> _entities = new();
    private readonly List<Entity> _roots = new();
    private readonly Dictionary<Entity, List<Entity>> _childrenOf = new();
    private readonly List<List<Entity>> _listPool = new();
    private string _filter = string.Empty;
    private Entity _dragged;

    /// <summary>Draws the window. Call between ImGui frame begin and render.</summary>
    public void Draw()
    {
        ImGui.SetNextWindowPos(new Vector2(10f, 40f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(300f, 520f), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Hierarchy"))
        {
            ImGui.End();
            return;
        }

        bool editable = canEdit(); // not while the game is playing: Stop restores the world
        ImGui.BeginDisabled(!editable);
        if (ImGui.Button("+ New")) newEntity();
        ImGui.SameLine();
        ImGui.BeginDisabled(selection.Selected.IsNull);
        if (ImGui.Button("+ Child")) newChild();
        ImGui.SameLine();
        if (ImGui.Button("Duplicate")) duplicate();
        ImGui.SameLine();
        if (ImGui.Button("Delete")) delete();
        ImGui.EndDisabled();
        ImGui.EndDisabled();

        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("##filter", "filter...", ref _filter, 64);
        ImGui.TextDisabled($"{world.EntityCount} entities");
        ImGui.Separator();

        // Copy first: a click or drop handler may change the world while the list is being drawn.
        BuildTree();

        ImGui.BeginChild("list", new Vector2(0f, -ImGui.GetFrameHeightWithSpacing()));
        if (_filter.Length > 0) DrawFiltered();
        else
        {
            foreach (var root in _roots) DrawNode(root, editable);
        }

        ImGui.EndChild();

        DrawUnparentStrip(editable);
        ImGui.End();
    }

    /// <summary>A click selects just the entity; Ctrl+click adds it to the selection or takes it out.</summary>
    private void Select(Entity entity)
    {
        if (ImGui.IsKeyDown(ImGuiKey.LeftCtrl) || ImGui.IsKeyDown(ImGuiKey.RightCtrl)) selection.Selection.Toggle(entity);
        else selection.Selected = entity;
    }

    private void BuildTree()
    {
        _entities.Clear();
        _roots.Clear();
        _childrenOf.Clear();
        int used = 0;
        foreach (var entity in world.Entities)
        {
            _entities.Add(entity);
            var parent = TransformHierarchy.GetParent(world, entity);
            if (parent.IsNull)
            {
                _roots.Add(entity);
                continue;
            }

            if (!_childrenOf.TryGetValue(parent, out var siblings))
            {
                if (used == _listPool.Count) _listPool.Add(new List<Entity>());
                siblings = _listPool[used++];
                siblings.Clear();
                _childrenOf[parent] = siblings;
            }

            siblings.Add(entity);
        }
    }

    private void DrawFiltered()
    {
        foreach (var entity in _entities)
        {
            string label = EntityLabel.Of(world, entity);
            if (!label.Contains(_filter, StringComparison.OrdinalIgnoreCase)) continue;

            if (ImGui.Selectable($"{label}##{entity.Id}", selection.Selection.Contains(entity)))
                Select(entity);
        }
    }

    private void DrawNode(Entity entity, bool editable)
    {
        bool hasChildren = _childrenOf.TryGetValue(entity, out var children) && children.Count > 0;
        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.DefaultOpen;
        if (!hasChildren) flags |= ImGuiTreeNodeFlags.Leaf;
        if (selection.Selection.Contains(entity)) flags |= ImGuiTreeNodeFlags.Selected;

        bool open = ImGui.TreeNodeEx($"{EntityLabel.Of(world, entity)}##{entity.Id}", flags);
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left) && !ImGui.IsItemToggledOpen()) Select(entity);

        if (editable) HandleDragAndDrop(entity);

        if (!open) return;

        if (hasChildren)
        {
            foreach (var child in children!) DrawNode(child, editable);
        }

        ImGui.TreePop();
    }

    /// <summary>Makes the last item a drag source (the entity) and a drop target (becomes the parent of what is dropped on it).</summary>
    private void HandleDragAndDrop(Entity entity)
    {
        if (ImGui.BeginDragDropSource())
        {
            _dragged = entity;
            ImGui.SetDragDropPayload(DragPayload, IntPtr.Zero, 0);
            ImGui.TextUnformatted(EntityLabel.Of(world, entity));
            ImGui.EndDragDropSource();
        }

        if (!ImGui.BeginDragDropTarget()) return;

        // A drop on itself or on one of its own descendants would make a loop, so those targets accept nothing.
        bool valid = world.IsAlive(_dragged) && !TransformHierarchy.IsSelfOrDescendantOf(world, entity, _dragged);
        if (valid && IsDropped(ImGui.AcceptDragDropPayload(DragPayload))) reparent(_dragged, entity);
        ImGui.EndDragDropTarget();
    }

    /// <summary>A strip under the list: dropping an entity here detaches it from its parent.</summary>
    private void DrawUnparentStrip(bool editable)
    {
        ImGui.TextDisabled("drop here to unparent");
        if (!editable || !ImGui.BeginDragDropTarget()) return;

        if (world.IsAlive(_dragged) && IsDropped(ImGui.AcceptDragDropPayload(DragPayload))) reparent(_dragged, Entity.Null);
        ImGui.EndDragDropTarget();
    }

    /// <summary>ImGui returns a null payload pointer until the mouse is released over the target; ImGui.NET exposes it only as a pointer.</summary>
    private static unsafe bool IsDropped(ImGuiPayloadPtr payload) => payload.NativePtr != null;
}
