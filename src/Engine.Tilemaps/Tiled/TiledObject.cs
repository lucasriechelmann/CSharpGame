namespace Engine.Tilemaps.Tiled;

/// <summary>An object placed on a Tiled object layer: a spawn point, a trigger area, a tile picture.</summary>
/// <param name="Id">The unique object id Tiled assigned.</param>
/// <param name="Name">The object name.</param>
/// <param name="Class">The object type or class text, empty when none.</param>
/// <param name="X">Horizontal position in map pixels.</param>
/// <param name="Y">Vertical position in map pixels, counted from the map top downwards (a tile object Y is its bottom edge).</param>
/// <param name="Width">Width in pixels (0 for a point).</param>
/// <param name="Height">Height in pixels (0 for a point).</param>
/// <param name="Gid">The global tile id for a tile object, 0 otherwise.</param>
/// <param name="IsPoint">True for a point object.</param>
/// <param name="Properties">Custom properties.</param>
public sealed record TiledObject(int Id, string Name, string Class, float X, float Y, float Width, float Height, uint Gid, bool IsPoint, TiledProperties Properties);
