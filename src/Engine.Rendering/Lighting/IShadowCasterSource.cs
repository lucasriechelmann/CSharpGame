using System.Numerics;

namespace Engine.Rendering;

/// <summary>
/// Supplies the walls that block light. <see cref="LightingEffect"/> asks every source for the walls near each light, so a source can
/// be boxes on entities, the solid tiles of a tile layer, or anything else that can list segments, and the effect never needs to know.
/// </summary>
public interface IShadowCasterSource
{
    /// <summary>Appends every edge that touches the world rectangle <paramref name="min"/>..<paramref name="max"/> (edges may extend beyond it).</summary>
    /// <param name="min">Lower-left corner of the area of interest.</param>
    /// <param name="max">Upper-right corner.</param>
    /// <param name="edges">The list to append to; never cleared by the source.</param>
    void CollectEdges(Vector2 min, Vector2 max, List<ShadowEdge> edges);
}
