using System.Numerics;

namespace Engine.Rendering;

/// <summary>
/// One wall of a shadow caster: a line segment with the solid on its <b>left</b> as you walk from <see cref="A"/> to <see cref="B"/> (so a
/// shape listed counter-clockwise has its edges facing outward on the right). The side matters: a light only casts shadow from the walls
/// that face away from it, which keeps the front of an object lit and puts the shadow behind it.
/// </summary>
/// <param name="A">Start point in world space.</param>
/// <param name="B">End point in world space.</param>
public readonly record struct ShadowEdge(Vector2 A, Vector2 B)
{
    /// <summary>The unit normal pointing out of the solid (to the right of A to B); zero for a zero-length edge.</summary>
    public Vector2 OutwardNormal
    {
        get
        {
            var d = B - A;
            float length = d.Length();
            return length > 0f ? new Vector2(d.Y, -d.X) / length : Vector2.Zero;
        }
    }
}
