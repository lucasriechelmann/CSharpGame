using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>
/// Picks which point lights matter for one mesh. The shader handles only <see cref="MeshShaderSource.MaxPointLights"/> at once, so a
/// scene with more lights chooses, per mesh, the ones that reach it and are strongest there. Pure and allocation-free.
/// </summary>
public static class PointLightSelector
{
    /// <summary>
    /// Fills <paramref name="chosen"/> with the indices (into <paramref name="positions"/>) of the lights that reach a bounding sphere,
    /// best first. A light reaches it when the gap between the two spheres is less than the light's radius; the best are the ones
    /// whose gap is the smallest fraction of their radius (the deepest inside the falloff).
    /// </summary>
    /// <param name="positions">World positions of all point lights.</param>
    /// <param name="radii">Radius of each light, parallel to <paramref name="positions"/>.</param>
    /// <param name="centre">Centre of the mesh's world-space bounding sphere.</param>
    /// <param name="meshRadius">Radius of that sphere.</param>
    /// <param name="chosen">Receives indices; its length is the most lights wanted.</param>
    /// <returns>How many indices were written.</returns>
    public static int Select(ReadOnlySpan<Vector3> positions, ReadOnlySpan<float> radii, Vector3 centre, float meshRadius, Span<int> chosen)
    {
        Span<float> scores = stackalloc float[chosen.Length];
        int count = 0;
        for (int i = 0; i < positions.Length; i++)
        {
            float reach = radii[i];
            if (reach <= 0f) continue;

            float gap = Vector3.Distance(positions[i], centre) - meshRadius;
            if (gap >= reach) continue;

            float score = MathF.Max(gap, 0f) / reach; // 0 = touching or inside the light's core, approaching 1 = at the edge

            // Insertion into the short sorted list; the loop moves worse entries down and drops the last when full.
            int slot = count;
            while (slot > 0 && scores[slot - 1] > score) slot--;
            if (slot >= chosen.Length) continue;

            int last = Math.Min(count, chosen.Length - 1);
            for (int move = last; move > slot; move--)
            {
                chosen[move] = chosen[move - 1];
                scores[move] = scores[move - 1];
            }

            chosen[slot] = i;
            scores[slot] = score;
            if (count < chosen.Length) count++;
        }

        return count;
    }
}
