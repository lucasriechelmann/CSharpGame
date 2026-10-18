using Engine.Core.Assets;

namespace Engine.Tilemaps;

/// <summary>Asset loader for <c>.layer.json</c> tile layer files.</summary>
public sealed class TileLayerLoader : IAssetLoader<TileLayer>
{
    /// <inheritdoc />
    public TileLayer Load(string fullPath) => TileLayerJson.Read(File.ReadAllText(fullPath));
}
