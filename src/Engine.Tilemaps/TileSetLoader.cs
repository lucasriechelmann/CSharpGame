using Engine.Core.Assets;
using Engine.Rendering;

namespace Engine.Tilemaps;

/// <summary>
/// Asset loader for <c>.tileset.json</c> files. It loads the atlas texture through the manager (so a texture loader must be
/// registered) and reads its size to work out the grid.
/// </summary>
/// <param name="assets">The manager that owns the atlas texture.</param>
public sealed class TileSetLoader(AssetManager assets) : IAssetLoader<TileSet>
{
    /// <inheritdoc />
    public TileSet Load(string fullPath)
    {
        var data = TileSetJson.Read(File.ReadAllText(fullPath));
        var texture = assets.Load<Texture2D>(data.Texture);
        var atlas = assets.Get(texture);
        return data.CreateTileSet(texture, atlas.Width, atlas.Height);
    }
}
