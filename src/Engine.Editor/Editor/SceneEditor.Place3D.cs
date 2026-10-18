using System.Numerics;
using Engine.Core.Assets;
using Engine.Rendering3D;
using Engine.Runtime.Editing;
using Engine.Runtime.Inspector;
using ImGuiNET;

namespace Engine.Editor;

// Placing 3D level pieces: Edit > Add 3D creates boxes, solid boxes and lights where the orbit camera is looking, snapped to a half-metre grid.
internal sealed partial class SceneEditor
{
    private const float PlaceGrid = 0.5f;
    private const string CubeMesh = "meshes/cube";
    private const string SphereMesh = "meshes/sphere";

    /// <summary>The "Add 3D" submenu (only in a 3D scene).</summary>
    private void DrawAdd3DMenu()
    {
        if (_hooks.Camera3D() == null || !ImGui.BeginMenu("Add 3D")) return;

        if (ImGui.MenuItem("Box")) AddBox("Box", CubeMesh, new Vector3(1f), solid: false);
        if (ImGui.MenuItem("Solid box (platform)")) AddBox("Platform", CubeMesh, new Vector3(3f, 0.5f, 3f), solid: true);
        if (ImGui.MenuItem("Sphere")) AddBox("Sphere", SphereMesh, new Vector3(1f), solid: false);
        ImGui.Separator();
        if (ImGui.MenuItem("Point light")) AddPointLight();
        ImGui.EndMenu();
    }

    /// <summary>Where new pieces go: the point the orbit camera looks at, on the grid.</summary>
    private Vector3 PlacementPoint() => EntityFactory3D.Snap(_orbit?.Target ?? Vector3.Zero, PlaceGrid);

    private void AddBox(string name, string meshName, Vector3 size, bool solid)
    {
        if (!TryLoadMesh(meshName, out var mesh)) return;

        var position = PlacementPoint() + new Vector3(0f, size.Y * 0.5f, 0f); // resting on the point, not centred on it
        var entity = EntityFactory3D.Box(_world, name, position, size, mesh, DefaultMaterial(), solid);
        Created(entity, $"Added {name}");
    }

    private void AddPointLight()
    {
        var entity = EntityFactory3D.PointLight(_world, PlacementPoint() + new Vector3(0f, 2f, 0f));
        Created(entity, "Added a point light");
    }

    /// <summary>Records a new entity as one undo step and selects it.</summary>
    private void Created(Engine.Core.Ecs.Entity entity, string message)
    {
        _history.Push(EntityLifetimeCommand.ForCreated(_history.Context, entity, EntityLabel.Of(_world, entity)));
        _inspector.Selected = entity;
        _log.Info("Editor", $"{message} ({entity})");
    }

    /// <summary>Finds the game's mesh of that name. A game that does not make it gets a log line instead of a crash.</summary>
    private bool TryLoadMesh(string name, out AssetHandle<Mesh> mesh)
    {
        try
        {
            mesh = _assets.Load<Mesh>(name);
            return true;
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException)
        {
            mesh = default;
            _log.Warning("Editor", $"This game has no mesh named '{name}' (add it with Assets.Add in Load, as the samples do).");
            return false;
        }
    }

    /// <summary>The first material file in the project's <c>materials</c> folder, so a new piece is not left with the plain default; invalid when there is none.</summary>
    private AssetHandle<Material> DefaultMaterial()
    {
        string folder = Path.Combine(_assets.RootDirectory, "materials");
        if (!Directory.Exists(folder)) return default;

        string? first = Directory.EnumerateFiles(folder, "*.material.json").Order(StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        return first == null ? default : _assets.Load<Material>("materials/" + Path.GetFileName(first));
    }
}
