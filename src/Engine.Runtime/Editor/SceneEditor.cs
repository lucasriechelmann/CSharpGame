using System.Numerics;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Logging;
using Engine.Rendering;
using Engine.Runtime.Editor.Tiles;
using Engine.Runtime.Editor.Undo;
using Engine.Runtime.Inspector;
using Engine.Tilemaps;
using ImGuiNET;

namespace Engine.Runtime.Editor;

/// <summary>
/// The scene editor: while it is on (F2) the simulation is frozen and the window gains a dockable layout, a menu bar, the
/// hierarchy and inspector panels, click-to-select in the scene, a move gizmo, and pan/zoom of the view. This file holds the commands
/// and the windows; <c>SceneEditor.Viewport.cs</c> handles the mouse in the scene itself.
/// </summary>
/// <remarks>
/// Rendering is untouched: the game still draws to the whole window and the editor's windows float over it (or dock to the
/// window's edges). The central dock node is pass-through, so clicks on empty space reach the scene.
/// Edits go straight to the live world for now; undo/redo (phase 12b) will route them through a command stack.
/// </remarks>
internal sealed partial class SceneEditor
{
    private readonly World _world;
    private readonly EntityInspector _inspector;
    private readonly HierarchyPanel _hierarchy;
    private readonly Logger _log;
    private readonly EditorHooks _hooks;
    private readonly CommandStack _history;
    private readonly AssetManager _assets;
    private readonly TilePainter _tiles;
    private string? _playSnapshot;

    /// <summary>Creates the editor.</summary>
    /// <param name="world">The world being edited.</param>
    /// <param name="assets">Resolves the tile layers and tile sets the painter edits.</param>
    /// <param name="inspector">Shows and edits the selected entity; also holds the selection.</param>
    /// <param name="log">Where file operations report to.</param>
    /// <param name="hooks">What the game provides: camera, scene file, save and load.</param>
    public SceneEditor(World world, AssetManager assets, EntityInspector inspector, Logger log, EditorHooks hooks)
    {
        _world = world;
        _assets = assets;
        _inspector = inspector;
        _log = log;
        _hooks = hooks;
        _history = new CommandStack(new EditContext(world));
        _tiles = new TilePainter(world, assets, inspector, _history);
        _hierarchy = new HierarchyPanel(world, inspector, () => !Playing, NewEntity, DuplicateSelected, DeleteSelected);
    }

    /// <summary>True while the editor is on (editing or playing): the editor's UI is drawn.</summary>
    public bool Active { get; private set; }

    /// <summary>True while the game is running inside the editor (between Play and Stop).</summary>
    public bool Playing => _playSnapshot != null;

    /// <summary>True while the editor is on and not playing: the simulation is frozen and the scene can be edited.</summary>
    public bool Editing => Active && !Playing;

    /// <summary>
    /// Starts the game from the current world. The world is saved first so <see cref="Stop"/> can put everything back exactly as it
    /// was edited, whatever the game does to it meanwhile.
    /// </summary>
    public void Play()
    {
        if (!Active || Playing) return;

        _playSnapshot = _hooks.Snapshot();
        _gizmo.End();
        _tiles.Cancel();
        _history.Clear();
        _inspector.History = null; // edits made while playing are live tweaks that Stop discards, so they are not undo steps
        _hooks.PlayStarted();
        _log.Info("Editor", "Play (the world is restored on Stop)");
    }

    /// <summary>Stops the game and restores the world as it was when Play was pressed.</summary>
    public void Stop()
    {
        if (_playSnapshot == null) return;

        string snapshot = _playSnapshot;
        _playSnapshot = null;
        _inspector.Selected = Entity.Null;
        _hooks.Restore(snapshot);
        _history.Clear(); // the world was rebuilt, so every entity handle in the history is stale
        _inspector.History = Active ? _history : null;
        _hooks.PlayStopped();
        _log.Info("Editor", "Stop (world restored)");
    }

    /// <summary>Turns the editor on or off. Turning it off while playing stops the game first.</summary>
    public void Toggle()
    {
        if (Active) Stop();

        Active = !Active;
        _gizmo.End();
        _tiles.Cancel();

        // History only means something against the world it was recorded on. The game runs freely while the editor is off, so start clean each time.
        _history.Clear();
        _inspector.History = Active ? _history : null;
        _log.Info("Editor", Active ? "Editor on (simulation frozen)" : "Editor off");
    }

    /// <summary>Draws the editor for one frame. Call between ImGui frame begin and render, only while <see cref="Active"/>.</summary>
    public void Draw()
    {
        DrawMenuBar();
        ImGui.DockSpaceOverViewport(0, ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);
        _hierarchy.Draw();
        _inspector.DrawDetailsWindow();
        _tiles.DrawPanel();

        HandleShortcuts();
        if (Playing) return; // the game owns the mouse and camera while it plays

        HandleViewport();
        _inspector.CommitPendingEdit();
        DrawSceneOverlay();
    }

    private void DrawMenuBar()
    {
        if (!ImGui.BeginMainMenuBar()) return;

        string? path = _hooks.ScenePath();
        ImGui.BeginDisabled(Playing); // file and edit commands change the scene, which is being played
        if (ImGui.BeginMenu("File"))
        {
            if (ImGui.MenuItem("Save scene", "Ctrl+S", false, path != null)) SaveScene();
            if (ImGui.MenuItem("Reload scene from disk", null, false, path != null)) ReloadScene();
            ImGui.Separator();
            if (ImGui.MenuItem("Close editor", "F2")) Toggle();
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Edit"))
        {
            if (ImGui.MenuItem(_history.CanUndo ? $"Undo {_history.UndoDescription}" : "Undo", "Ctrl+Z", false, _history.CanUndo)) Undo();
            if (ImGui.MenuItem(_history.CanRedo ? $"Redo {_history.RedoDescription}" : "Redo", "Ctrl+Y", false, _history.CanRedo)) Redo();
            ImGui.Separator();
            if (ImGui.MenuItem("New entity", "Ctrl+N")) NewEntity();
            if (ImGui.MenuItem("Duplicate", "Ctrl+D", false, !_inspector.Selected.IsNull)) DuplicateSelected();
            if (ImGui.MenuItem("Delete", "Del", false, !_inspector.Selected.IsNull)) DeleteSelected();
            ImGui.EndMenu();
        }

        ImGui.EndDisabled();

        if (ImGui.MenuItem(Playing ? "Stop" : "Play", "F3")) TogglePlay();

        ImGui.TextDisabled(Playing
            ? "  Playing: changes are discarded when you press Stop."
            : "  Editor: simulation frozen. Middle-drag pans, wheel zooms, Ctrl snaps the gizmo to 8 units.");
        ImGui.EndMainMenuBar();
    }

    private void HandleShortcuts()
    {
        var io = ImGui.GetIO();
        if (io.WantTextInput) return; // typing in a field

        bool ctrl = CtrlHeld();
        // F3, not a Ctrl chord: the key would also reach the game while it plays (Ctrl+P is the sample's pause key too).
        if (ImGui.IsKeyPressed(ImGuiKey.F3, false)) TogglePlay();
        if (Playing) return;

        bool shift = ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift);
        bool editing = _gizmo.IsDragging || _tiles.IsStroking || ImGui.IsAnyItemActive(); // do not pull the world out from under a drag
        if (ctrl && ImGui.IsKeyPressed(ImGuiKey.Z, false) && !editing) { if (shift) Redo(); else Undo(); }
        else if (ctrl && ImGui.IsKeyPressed(ImGuiKey.Y, false) && !editing) Redo();
        else if (ctrl && ImGui.IsKeyPressed(ImGuiKey.S, false)) SaveScene();
        else if (ctrl && ImGui.IsKeyPressed(ImGuiKey.D, false)) DuplicateSelected();
        else if (ctrl && ImGui.IsKeyPressed(ImGuiKey.N, false)) NewEntity();
        else if (ImGui.IsKeyPressed(ImGuiKey.Delete, false)) DeleteSelected();
        else if (ImGui.IsKeyPressed(ImGuiKey.B, false)) _tiles.TogglePaintMode();
    }

    /// <summary>
    /// Whether a Control key is down. Read from the keys themselves rather than <c>io.KeyCtrl</c>, which ImGui refreshes once per frame
    /// and can still report false on the frame a quick Ctrl+key chord arrives.
    /// </summary>
    private static bool CtrlHeld() => ImGui.IsKeyDown(ImGuiKey.LeftCtrl) || ImGui.IsKeyDown(ImGuiKey.RightCtrl);

    /// <summary>Creates a named entity at the middle of the view and selects it.</summary>
    private void NewEntity()
    {
        var centre = _hooks.Camera()?.Position ?? Vector2.Zero;
        var entity = _world.CreateEntity();
        _world.Add(entity, Name.Of("Entity"));
        _world.Add(entity, Transform.At(centre.X, centre.Y));
        _history.Push(EntityLifetimeCommand.ForCreated(_history.Context, entity, EntityLabel.Of(_world, entity)));
        _inspector.Selected = entity;
        _log.Info("Editor", $"Created {entity}");
    }

    private void TogglePlay()
    {
        if (Playing) Stop();
        else Play();
    }

    /// <summary>Reverts the latest change and selects what it touched.</summary>
    private void Undo()
    {
        _inspector.CommitPendingEdit();
        var command = _history.Undo();
        if (command == null) return;

        _inspector.Selected = command.Affected(_history.Context);
        _log.Info("Editor", $"Undo: {command.Description}");
    }

    /// <summary>Reapplies the latest undone change and selects what it touched.</summary>
    private void Redo()
    {
        var command = _history.Redo();
        if (command == null) return;

        _inspector.Selected = command.Affected(_history.Context);
        _log.Info("Editor", $"Redo: {command.Description}");
    }

    /// <summary>Copies the selected entity, a little to the side, and selects the copy.</summary>
    private void DuplicateSelected()
    {
        var source = _inspector.Selected;
        if (source.IsNull) return;

        float step = 16f / (_hooks.Camera()?.Zoom ?? 1f);
        var copy = EntityCloner.Duplicate(_world, source, new Vector2(step, -step));
        _history.Push(EntityLifetimeCommand.ForCreated(_history.Context, copy, EntityLabel.Of(_world, copy)));
        _inspector.Selected = copy;
        _log.Info("Editor", $"Duplicated {source}");
    }

    private void DeleteSelected()
    {
        var entity = _inspector.Selected;
        if (entity.IsNull) return;

        _inspector.CommitPendingEdit();
        _history.Execute(EntityLifetimeCommand.ForDeleted(_history.Context, entity, EntityLabel.Of(_world, entity)));
        _inspector.Selected = Entity.Null;
        _log.Info("Editor", $"Deleted {entity}");
    }

    private void SaveScene()
    {
        string? path = _hooks.ScenePath();
        if (path == null) return;

        _hooks.Save(path);
        _log.Info("Editor", $"Saved {_world.EntityCount} entities to {path} (prefab instances as prefab + overrides)");
        SaveDirtyTileLayers();
    }

    /// <summary>Writes every tile layer that was painted since it was last saved, each to the file the game names for it.</summary>
    private void SaveDirtyTileLayers()
    {
        foreach (var handle in _history.Context.DirtyTileLayers)
        {
            string name = _assets.NameOf(handle);
            string file = _hooks.AssetPath(name);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, TileLayerJson.Write(_assets.Get(handle)));
            _hooks.AssetWritten(file);
            _log.Info("Editor", $"Saved tile layer {name} to {file}");
        }

        _history.Context.DirtyTileLayers.Clear();
    }

    private void ReloadScene()
    {
        string? path = _hooks.ScenePath();
        if (path == null) return;

        _inspector.Selected = Entity.Null;
        _hooks.Load(path);
        _history.Clear(); // every entity handle in the history is stale after the world was replaced
        _log.Info("Editor", $"Reloaded {_world.EntityCount} entities from {path}");
    }
}
