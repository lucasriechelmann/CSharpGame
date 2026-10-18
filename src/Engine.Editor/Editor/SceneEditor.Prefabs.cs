using Engine.Core.Ecs;
using Engine.Runtime.Editing;
using Engine.Runtime.Inspector;
using ImGuiNET;

namespace Engine.Editor;

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
            _prefabNameInput = FreePrefabName(_world.TryGet<Engine.Core.Components.Name>(selected, out var name) ? name.Value : "entity");
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
        ImGui.InputText("##prefabname", ref _prefabNameInput, 256, ImGuiInputTextFlags.AutoSelectAll);
        bool exists = File.Exists(PrefabPath(_prefabNameInput.Trim()));
        if (exists) ImGui.TextColored(new System.Numerics.Vector4(1f, 0.75f, 0.35f, 1f), "A prefab with this name exists and will be replaced.");
        if (ImGui.Button(exists ? "Replace" : "Save"))
        {
            SaveSelectedAsPrefab(_prefabNameInput.Trim());
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    /// <summary>A prefab file name based on an entity's label that does not exist yet: <c>prefabs/coin.prefab.json</c>, or <c>prefabs/coin_2.prefab.json</c> when that is taken.</summary>
    private string FreePrefabName(string label)
    {
        string stem = System.Text.RegularExpressions.Regex.Replace(label.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');
        if (stem.Length == 0) stem = "entity";
        string name = $"prefabs/{stem}.prefab.json";
        for (int n = 2; File.Exists(PrefabPath(name)); n++) name = $"prefabs/{stem}_{n}.prefab.json";
        return name;
    }

    private string PrefabPath(string assetName) => Path.Combine(_assets.RootDirectory, assetName);

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
