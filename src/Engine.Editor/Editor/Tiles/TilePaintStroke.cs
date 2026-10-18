using Engine.Core.Assets;
using Engine.Runtime.Editing;
using Engine.Tilemaps;

namespace Engine.Editor.Tiles;

/// <summary>
/// One press-drag-release of the paint tool. It changes the layer cell by cell as the mouse moves (so the artist sees the result at
/// once), remembers each cell's original id the first time it is touched, and at the end turns the whole stroke into a single undo step.
/// </summary>
public sealed class TilePaintStroke(AssetHandle<TileLayer> handle, TileLayer layer)
{
    // Insertion order is kept so redo applies cells in the order they were painted.
    private readonly List<(int X, int Y, int Before)> _touched = new();
    private readonly HashSet<(int, int)> _seen = new();

    /// <summary>True when nothing has been painted yet, or every painted cell ended up as it started.</summary>
    public bool IsEmpty => Changes().Count == 0;

    /// <summary>Sets a cell. The first change to a cell records its original id.</summary>
    /// <param name="x">Cell column.</param>
    /// <param name="y">Cell row.</param>
    /// <param name="id">The tile id to put there; 0 erases.</param>
    /// <returns>True when the cell actually changed.</returns>
    public bool Paint(int x, int y, int id)
    {
        if (id < 0) throw new ArgumentOutOfRangeException(nameof(id), id, "Tile ids cannot be negative.");

        int current = layer.GetTile(x, y);
        if (current == id) return false;

        if (_seen.Add((x, y))) _touched.Add((x, y, current));
        layer.SetTile(x, y, id);
        return true;
    }

    /// <summary>The recorded cells that differ from how they started (a cell painted and then restored by the same stroke is dropped).</summary>
    public IReadOnlyList<TileChange> Changes()
    {
        var changes = new List<TileChange>();
        foreach (var (x, y, before) in _touched)
        {
            int after = layer.GetTile(x, y);
            if (after != before) changes.Add(new TileChange(x, y, before, after));
        }

        return changes;
    }

    /// <summary>The undo step for the finished stroke. The changes are already in the layer, so record it with <c>CommandStack.Push</c>.</summary>
    /// <param name="label">What to call the layer in the description.</param>
    public PaintTilesCommand ToCommand(string label) => new(handle, layer, Changes(), label);
}
