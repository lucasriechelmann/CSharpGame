using Engine.Core.Ecs;
using Engine.Runtime.Editor.Undo;
using Engine.Runtime.Inspector;
using ImGuiNET;

namespace Engine.Runtime.Editor;

// Prefab commands: save the selected entity as a prefab, or revert an instance to its prefab.
internal sealed partial class SceneEditor
{
    private const string SavePrefabPopup = "Save as prefab";

    private string _prefabNameInput = "";
    private bool _openPrefabPopup;

    /// <summary>The Edit-menu entries for prefabs. Call inside the Edit menu.</summary>
    private void DrawPrefabMenuItems()
    {
        var selected = _inspector.Selected;
        bool has = !selected.IsNull && _world.IsAlive(selected);
        if (ImGui.MenuItem("Save as prefab...", null, false, has))
        {
            _prefabNameInput = $"prefabs/{EntityLabel.Of(_world, selected).ToLowerInvariant().Replace(' ', '_')}.prefab.json";
            _openPrefabPopup = true;
        }

        if (ImGui.MenuItem("Revert to prefab", null, false, has && _hooks.PrefabOf(selected) != null)) RevertSelectedToPrefab();
    }

    /// <summary>Draws the file-name popup that Save as prefab opens. Call every frame, outside any menu.</summary>
    private void DrawPrefabPopup()
    {
        if (_openPrefabPopup)
        {
            ImGui.OpenPopup(SavePrefabPopup);
            _openPrefabPopup = false;
        }

        if (!ImGui.BeginPopupModal(SavePrefabPopup, ImGuiWindowFlags.AlwaysAutoResize)) return;

        ImGui.TextUnformatted("File, relative to the asset root:");
        ImGui.InputText("##prefabname", ref _prefabNameInput, 256);
        if (ImGui.Button("Save"))
        {
            SaveSelectedAsPrefab(_prefabNameInput.Trim());
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void SaveSelectedAsPrefab(string name)
    {
        var entity = _inspector.Selected;
        if (entity.IsNull || !_world.IsAlive(entity) || name.Length == 0) return;

        _hooks.SaveAsPrefab(entity, name);
        _log.Info("Editor", $"Saved {EntityLabel.Of(_world, entity)} as prefab {name}");
    }

    private void RevertSelectedToPrefab()
    {
        var entity = _inspector.Selected;
        if (entity.IsNull || !_world.IsAlive(entity)) return;

        _history.Execute(new RevertToPrefabCommand(_history.Context, entity, _hooks.RevertToPrefab, EntityLabel.Of(_world, entity)));
        _log.Info("Editor", $"Reverted {EntityLabel.Of(_world, entity)} to {_hooks.PrefabOf(entity)}");
    }
}
