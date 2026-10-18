using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Scenes;
using ImGuiNET;

namespace Engine.Runtime.Inspector;

/// <summary>
/// The "Inspector" debug window: a list of every entity on the left, and on the right the selected entity's components, each
/// editable through <see cref="FieldEditor"/>. Components can be added (any registered one) and removed, and entities created
/// and destroyed, so levels can be poked at while the game runs.
/// </summary>
/// <remarks>
/// Editing happens immediately on the live world: values you drag change the very next frame. Structural changes (destroy,
/// add, remove) are collected during drawing and applied afterwards, because the entity list is being enumerated while it is drawn.
/// </remarks>
internal sealed partial class EntityInspector(World world, ComponentRegistry registry)
{
    private readonly List<Type> _componentTypes = new();
    private readonly List<Entity> _entities = new();
    private string _filter = string.Empty;
    private Entity _selected = Entity.Null;

    /// <summary>The entity whose components are shown; <see cref="Entity.Null"/> when nothing is selected or it was destroyed.</summary>
    public Entity Selected
    {
        get => world.IsAlive(_selected) ? _selected : Entity.Null;
        set => _selected = value;
    }

    /// <summary>Draws the window. Call between ImGui frame begin and render.</summary>
    public void Draw()
    {
        ImGui.SetNextWindowPos(new Vector2(580f, 380f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(520f, 320f), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Inspector"))
        {
            ImGui.End();
            return;
        }

        DrawEntityList();
        ImGui.SameLine();
        DrawDetails();
        ImGui.End();
    }

    /// <summary>
    /// Draws only the selected entity's components in a window of their own, for the editor, where the hierarchy panel
    /// does the selecting.
    /// </summary>
    public void DrawDetailsWindow()
    {
        ImGui.SetNextWindowPos(new Vector2(960f, 40f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(310f, 520f), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Inspector"))
            DrawDetails();
        ImGui.End();
    }

    /// <summary>The label shown for an entity: its <see cref="Name"/> component if it has one, otherwise its id.</summary>
    private string LabelOf(Entity entity) => EntityLabel.Of(world, entity);
}
