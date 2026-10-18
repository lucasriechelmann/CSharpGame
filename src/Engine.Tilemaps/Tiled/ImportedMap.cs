using System.Numerics;

namespace Engine.Tilemaps.Tiled;

/// <summary>A Tiled map converted to engine data: tile sets, tile layers and objects, all in world units with Y pointing up.</summary>
/// <param name="TileSets">The tile sets, ready to load.</param>
/// <param name="Layers">The tile layers, bottom first.</param>
/// <param name="Objects">The objects of every object layer.</param>
/// <param name="WorldSize">The map size in world units (zero for an infinite map).</param>
/// <param name="TileSize">The side of one cell in world units.</param>
public sealed record ImportedMap(
    IReadOnlyList<ImportedTileSet> TileSets,
    IReadOnlyList<ImportedLayer> Layers,
    IReadOnlyList<ImportedObject> Objects,
    Vector2 WorldSize,
    float TileSize);
