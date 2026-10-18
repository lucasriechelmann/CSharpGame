using Engine.Core.Assets;
using Engine.Core.Ecs;
using Engine.Runtime.Editor.Tiles;
using Engine.Tilemaps;

namespace Engine.Runtime.Editor.Undo;

/// <summary>
/// A stroke of the tile paint tool as one undo step: a list of cells with their ids before and after. Tile layers are shared assets,
/// not entities, so the command holds the layer itself; it also marks the layer as having unsaved changes.
/// </summary>
public sealed class PaintTilesCommand : IEditCommand
{
    private readonly AssetHandle<TileLayer> _handle;
    private readonly TileLayer _layer;
    private readonly IReadOnlyList<TileChange> _changes;

    /// <summary>Creates the command.</summary>
    /// <param name="handle">The layer's asset handle, so Save knows which file to write.</param>
    /// <param name="layer">The layer that was painted.</param>
    /// <param name="changes">The cells that changed, in painting order.</param>
    /// <param name="label">What to call the layer in the description.</param>
    public PaintTilesCommand(AssetHandle<TileLayer> handle, TileLayer layer, IReadOnlyList<TileChange> changes, string label)
    {
        _handle = handle;
        _layer = layer;
        _changes = changes;
        Description = changes.Count == 1 ? $"Paint a tile on {label}" : $"Paint {changes.Count} tiles on {label}";
    }

    /// <summary>Number of cells this step changes.</summary>
    public int Count => _changes.Count;

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public void Do(EditContext context)
    {
        foreach (var change in _changes) _layer.SetTile(change.X, change.Y, change.After);
        context.DirtyTileLayers.Add(_handle);
    }

    /// <inheritdoc />
    public void Undo(EditContext context)
    {
        // Reverse order, so a cell that appears twice ends on its original value.
        for (int i = _changes.Count - 1; i >= 0; i--) _layer.SetTile(_changes[i].X, _changes[i].Y, _changes[i].Before);
        context.DirtyTileLayers.Add(_handle);
    }

    /// <inheritdoc />
    public Entity Affected(EditContext context) => Entity.Null;
}
