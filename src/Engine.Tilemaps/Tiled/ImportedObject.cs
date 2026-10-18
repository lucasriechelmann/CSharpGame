using System.Numerics;

namespace Engine.Tilemaps.Tiled;

/// <summary>A Tiled object converted to world space, for the game to turn into whatever it stands for (a spawn point, a trigger, a decoration).</summary>
/// <param name="Layer">The name of the object layer it was on.</param>
/// <param name="Name">The object name.</param>
/// <param name="Class">The object type or class text.</param>
/// <param name="Position">World position (Y up). For a rectangle this is its <b>bottom-left</b> corner; for a point, the point; for a tile object, the bottom-left of the tile.</param>
/// <param name="Size">World size (zero for a point).</param>
/// <param name="Gid">The global tile id for a tile object, 0 otherwise.</param>
/// <param name="IsPoint">True for a point object.</param>
/// <param name="Properties">The custom properties.</param>
public sealed record ImportedObject(string Layer, string Name, string Class, Vector2 Position, Vector2 Size, uint Gid, bool IsPoint, TiledProperties Properties);
