using System.Numerics;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Rendering;
using Engine.Runtime.Editing;
using Engine.Runtime.Inspector;
using Engine.Tilemaps;
using ImGuiNET;

namespace Engine.Editor.Tiles;

/// <summary>
/// The tile paint tool. With a tile layer entity selected, the "Tiles" window shows the tile set as a palette and a Paint mode
/// switch; in paint mode the left mouse button paints the chosen tile into the layer under the cursor and the right button erases,
/// and each press-drag-release is one undo step (<see cref="TilePaintStroke"/>). The ImGui half of the painter: the cell maths and
/// the undoable stroke are pure and tested.
/// </summary>
internal sealed class TilePainter(World world, AssetManager assets, EntityInspector inspector, CommandStack history)
{
    private const float PaletteTileSize = 36f;

    private int _selectedTile = 1;
    private int _flags;
    private TileBrush _brush = TileBrush.Pencil;
    private (int X, int Y)? _anchor;
    private TilePaintStroke? _stroke;
    private ImGuiMouseButton _strokeButton;
    private (int X, int Y)? _lastCell;
    private AssetHandle<TileLayer> _strokeHandle;
    private string _strokeLayerName = string.Empty;

    /// <summary>True when the paint tool is switched on (it only acts while a tile layer is selected).</summary>
    public bool PaintMode { get; private set; }

    /// <summary>True when a stroke is in progress.</summary>
    public bool IsStroking => _stroke != null;

    /// <summary>True when the mouse in the scene should paint rather than select: paint mode is on and a tile layer is selected.</summary>
    public bool IsPainting => PaintMode && TryGetTarget(out _);

    /// <summary>Switches paint mode on or off (the B key).</summary>
    public void TogglePaintMode()
    {
        EndStroke();
        PaintMode = !PaintMode;
    }

    /// <summary>Ends any stroke in progress. Call when the editor turns off or the world is replaced.</summary>
    public void Cancel() => EndStroke();

    /// <summary>The selected entity's tile layer, tile set and world origin; false when the selection is not a tile layer.</summary>
    private bool TryGetTarget(out Target target)
    {
        target = default;
        var entity = inspector.Selected;
        if (entity.IsNull || !world.TryGet<TileLayerRenderer>(entity, out var renderer) || !world.TryGet<Transform>(entity, out var transform))
            return false;
        if (!assets.TryGet(renderer.Layer, out var layer) || !assets.TryGet(renderer.TileSet, out var tileSet)) return false;

        target = new Target(renderer.Layer, layer, renderer.TileSet, tileSet, new Vector2(transform.Position.X, transform.Position.Y));
        return true;
    }

    /// <summary>Draws the "Tiles" window.</summary>
    public void DrawPanel()
    {
        ImGui.SetNextWindowPos(new Vector2(260f, 570f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(330f, 220f), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Tiles"))
        {
            ImGui.End();
            return;
        }

        if (!TryGetTarget(out var target))
        {
            ImGui.TextWrapped("Select a tile layer (an entity with a TileLayerRenderer) in the hierarchy to paint on it.");
            ImGui.End();
            return;
        }

        string name = assets.NameOf(target.LayerHandle);
        bool dirty = history.Context.DirtyTileLayers.Contains(target.LayerHandle);
        ImGui.Text($"{name}{(dirty ? " *" : string.Empty)}   {target.Layer.TileCount} tiles");

        bool paint = PaintMode;
        if (ImGui.Checkbox("Paint mode (B)", ref paint)) TogglePaintMode();
        ImGui.TextDisabled("Left button paints, right button erases.");
        DrawBrushControls();

        DrawPalette(target.TileSet);
        ImGui.End();
    }

    private void DrawPalette(TileSet tileSet)
    {
        if (ImGui.Button(_selectedTile == 0 ? "[Eraser]" : "Eraser")) _selectedTile = 0;

        var atlas = assets.Get(tileSet.Texture);
        for (int id = 1; id <= tileSet.TileCount; id++)
        {
            var rect = tileSet.GetSourceRect(id);
            var uv0 = new Vector2(rect.X / atlas.Width, rect.Y / atlas.Height);
            var uv1 = new Vector2((rect.X + rect.Width) / atlas.Width, (rect.Y + rect.Height) / atlas.Height);

            if ((id - 1) % Math.Max(tileSet.Columns, 1) != 0) ImGui.SameLine();
            ImGui.PushID(id);
            bool selected = id == _selectedTile;
            if (selected) ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1f, 0.85f, 0.2f, 1f));
            if (ImGui.ImageButton("##tile", (IntPtr)atlas.Handle, new Vector2(PaletteTileSize), uv0, uv1)) _selectedTile = id;
            if (selected) ImGui.PopStyleColor();
            ImGui.PopID();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Tile {id}{(tileSet.IsSolid(id) ? " (solid)" : string.Empty)}");
        }
    }

    /// <summary>
    /// Paints or erases under the mouse. Call every frame while <see cref="IsPainting"/>; a stroke ends (and is recorded as one undo
    /// step) when the button is released.
    /// </summary>
    /// <param name="camera">The scene camera.</param>
    /// <param name="mouse">Mouse position in screen pixels.</param>
    /// <param name="uiHasMouse">True when the mouse is over an editor window, which must not start a stroke.</param>
    public void HandleMouse(Camera2D camera, Vector2 mouse, bool uiHasMouse)
    {
        if (!TryGetTarget(out var target))
        {
            EndStroke();
            return;
        }

        var (cellX, cellY) = TileCells.FromWorld(camera.ScreenToWorld(mouse), target.Origin, target.Layer.TileSize);

        if (_stroke == null && !uiHasMouse)
        {
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) BeginStroke(target, ImGuiMouseButton.Left);
            else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) BeginStroke(target, ImGuiMouseButton.Right);

            if (_stroke != null && _brush == TileBrush.Rectangle) _anchor = (cellX, cellY);
            if (_stroke != null && _brush == TileBrush.Fill)
            {
                FillFrom(target, camera, cellX, cellY);
                EndStroke();
                return;
            }
        }

        if (_stroke == null) return;
        if (!ImGui.IsMouseDown(_strokeButton))
        {
            if (_brush == TileBrush.Rectangle && _anchor is var (ax, ay))
            {
                foreach (var (x, y) in TileShapes.Rectangle(ax, ay, cellX, cellY)) _stroke.Paint(x, y, ValueFor(_strokeButton));
            }

            EndStroke();
            return;
        }

        if (_brush != TileBrush.Pencil) return; // a rectangle only previews while the button is down
        int id = ValueFor(_strokeButton);
        var (fromX, fromY) = _lastCell ?? (cellX, cellY);
        foreach (var (x, y) in TileCells.Line(fromX, fromY, cellX, cellY)) _stroke.Paint(x, y, id);
        _lastCell = (cellX, cellY);
    }

    /// <summary>The raw tile value a button paints: the right button erases, the left paints the chosen tile with the chosen flips.</summary>
    private int ValueFor(ImGuiMouseButton button) => button == ImGuiMouseButton.Right || _selectedTile == 0 ? 0 : TileId.With(_selectedTile, _flags);

    /// <summary>Bucket fill inside the part of the layer the camera can see.</summary>
    private void FillFrom(Target target, Camera2D camera, int cellX, int cellY)
    {
        camera.GetVisibleBounds(out var min, out var max);
        var (minX, minY) = TileCells.FromWorld(min, target.Origin, target.Layer.TileSize);
        var (maxX, maxY) = TileCells.FromWorld(max, target.Origin, target.Layer.TileSize);
        int value = ValueFor(_strokeButton);
        foreach (var (x, y) in TileShapes.FloodFill(target.Layer, cellX, cellY, minX, minY, maxX, maxY)) _stroke!.Paint(x, y, value);
    }

    /// <summary>The brush choice and flip toggles in the Tiles window.</summary>
    private void DrawBrushControls()
    {
        int brush = (int)_brush;
        ImGui.RadioButton("Pencil", ref brush, (int)TileBrush.Pencil);
        ImGui.SameLine();
        ImGui.RadioButton("Rectangle", ref brush, (int)TileBrush.Rectangle);
        ImGui.SameLine();
        ImGui.RadioButton("Fill", ref brush, (int)TileBrush.Fill);
        _brush = (TileBrush)brush;

        bool h = (_flags & TileId.FlipHorizontal) != 0, v = (_flags & TileId.FlipVertical) != 0, d = (_flags & TileId.FlipDiagonal) != 0;
        if (ImGui.Checkbox("Flip X", ref h)) _flags ^= TileId.FlipHorizontal;
        ImGui.SameLine();
        if (ImGui.Checkbox("Flip Y", ref v)) _flags ^= TileId.FlipVertical;
        ImGui.SameLine();
        if (ImGui.Checkbox("Diagonal", ref d)) _flags ^= TileId.FlipDiagonal;
    }

    /// <summary>Outlines the cell under the mouse so the artist sees where a click would paint.</summary>
    /// <param name="camera">The scene camera.</param>
    /// <param name="mouse">Mouse position in screen pixels.</param>
    /// <param name="draw">Where the outline is drawn (the scene view, in view coordinates).</param>
    public void DrawCursor(Camera2D camera, Vector2 mouse, ViewDraw draw)
    {
        if (!TryGetTarget(out var target)) return;

        var (x, y) = TileCells.FromWorld(camera.ScreenToWorld(mouse), target.Origin, target.Layer.TileSize);
        if (_stroke != null && _brush == TileBrush.Rectangle && _anchor is var (ax, ay))
        {
            var (lo, hi) = TileCells.Bounds(Math.Min(ax, x), Math.Min(ay, y), target.Origin, target.Layer.TileSize);
            var (lo2, hi2) = TileCells.Bounds(Math.Max(ax, x), Math.Max(ay, y), target.Origin, target.Layer.TileSize);
            var p = camera.WorldToScreen(Vector2.Min(lo, lo2));
            var q = camera.WorldToScreen(Vector2.Max(hi, hi2));
            draw.AddRect(Vector2.Min(p, q), Vector2.Max(p, q), ImGui.GetColorU32(new Vector4(1f, 0.9f, 0.3f, 1f)), 2f);
        }

        var (min, max) = TileCells.Bounds(x, y, target.Origin, target.Layer.TileSize);
        var a = camera.WorldToScreen(min);
        var b = camera.WorldToScreen(max);
        draw.AddRect(Vector2.Min(a, b), Vector2.Max(a, b), ImGui.GetColorU32(new Vector4(0.3f, 1f, 1f, 1f)), 2f);
    }

    private void BeginStroke(Target target, ImGuiMouseButton button)
    {
        _stroke = new TilePaintStroke(target.LayerHandle, target.Layer);
        _strokeHandle = target.LayerHandle; // kept so the stroke is recorded even if the selection changes before the button is released
        _strokeLayerName = assets.NameOf(target.LayerHandle);
        _strokeButton = button;
        _lastCell = null;
    }

    private void EndStroke()
    {
        var stroke = _stroke;
        _stroke = null;
        _anchor = null;
        _lastCell = null;
        if (stroke == null || stroke.IsEmpty) return;

        history.Push(stroke.ToCommand(_strokeLayerName));
        history.Context.DirtyTileLayers.Add(_strokeHandle);
    }

    private readonly record struct Target(AssetHandle<TileLayer> LayerHandle, TileLayer Layer, AssetHandle<TileSet> TileSetHandle, TileSet TileSet, Vector2 Origin);
}
