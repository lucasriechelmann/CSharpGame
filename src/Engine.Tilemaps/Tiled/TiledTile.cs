namespace Engine.Tilemaps.Tiled;

/// <summary>Extra information Tiled stores for one tile of a tile set: its animation and custom properties.</summary>
/// <param name="Id">The tile id inside its tile set (0-based).</param>
/// <param name="Class">The tile type or class text, empty when none.</param>
/// <param name="Properties">Custom properties; a boolean <c>solid</c> is how a map marks collision.</param>
/// <param name="Animation">Animation frames, empty for a still tile.</param>
public sealed record TiledTile(int Id, string Class, TiledProperties Properties, IReadOnlyList<TiledAnimationFrame> Animation);
