using System.Numerics;
using System.Text;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Runtime.Editing;
using Engine.Runtime.Inspector;
using ImGuiNET;

namespace Engine.Editor;

// Dropping a prefab from the Assets window onto the Scene panel.
internal sealed partial class SceneEditor
{
    /// <summary>The distance in front of the 3D camera a dropped prefab lands at when the mouse ray misses the ground.</summary>
    private const float DropDistance3D = 6f;

    private readonly HashSet<string> _unloadablePictures = new();

    /// <summary>Loads a picture of the project for the Assets window's hover preview (loaded once, shared with the game). Null for one that cannot be loaded.</summary>
    private Engine.Rendering.Texture2D? LoadThumbnail(string assetName)
    {
        if (_unloadablePictures.Contains(assetName)) return null;

        try
        {
            return _assets.Get(_assets.Load<Engine.Rendering.Texture2D>(assetName));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or InvalidDataException)
        {
            _unloadablePictures.Add(assetName); // once: a hover would otherwise retry (and log) every frame
            _log.Warning("Editor", $"No thumbnail for {assetName}: {ex.Message}");
            return null;
        }
    }

    /// <summary>Call right after the scene image is drawn: turns a prefab dropped on it into a new, selected, undoable entity under the mouse.</summary>
    private unsafe void AcceptPrefabDrop()
    {
        if (!ImGui.BeginDragDropTarget()) return;

        var payload = ImGui.AcceptDragDropPayload(AssetBrowserPanel.PrefabPayload);
        if (payload.NativePtr != null)
        {
            string prefab = Encoding.UTF8.GetString((byte*)payload.Data, payload.DataSize);
            PlacePrefab(prefab, _viewport.ToView(ImGui.GetMousePos()));
        }

        ImGui.EndDragDropTarget();
    }

    /// <summary>Creates an instance of a prefab at a point of the scene view and selects it. One undo step.</summary>
    private void PlacePrefab(string prefab, Vector2 viewPoint)
    {
        Entity entity;
        try
        {
            entity = _hooks.Instantiate(prefab);
        }
        catch (InvalidOperationException ex)
        {
            _log.Error("Editor", $"Cannot place {prefab}: {ex.Message}");
            return;
        }

        if (_world.TryGet<Transform>(entity, out var transform))
        {
            var position = DropPosition(viewPoint, transform.Position.Z);
            transform.Position = position;
            _history.Context.SetComponent(entity, typeof(Transform), transform); // also clears PreviousTransform, so it is drawn where it was dropped
        }

        _history.Push(EntityLifetimeCommand.ForCreated(_history.Context, entity, EntityLabel.Of(_world, entity)));
        _inspector.Selected = entity;
        _log.Info("Editor", $"Placed {prefab}");
    }

    /// <summary>Where a drop lands in the world: under the mouse in 2D (keeping the prefab's own Z, its draw order); on the ground plane in 3D, or in front of the camera when the ray misses it.</summary>
    private Vector3 DropPosition(Vector2 viewPoint, float keepZ)
    {
        var camera3D = _hooks.Camera3D();
        if (camera3D != null)
        {
            var ray = camera3D.ScreenPointToRay(viewPoint);
            if (MathF.Abs(ray.Direction.Y) > 1e-4f)
            {
                float t = -ray.Origin.Y / ray.Direction.Y;
                if (t > 0f) return ray.GetPoint(t);
            }

            return ray.GetPoint(DropDistance3D);
        }

        var camera = _hooks.Camera();
        if (camera == null) return new Vector3(0f, 0f, keepZ);

        var world = camera.ScreenToWorld(viewPoint);
        return new Vector3(world.X, world.Y, keepZ);
    }
}
