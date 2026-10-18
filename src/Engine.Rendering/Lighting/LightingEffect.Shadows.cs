using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine.Rendering;

// Shadows: each light that has walls in reach stamps their shadow shapes into the light map's stencil buffer, then draws itself only where the stencil is still clear.
public sealed partial class LightingEffect
{
    /// <summary>Gathers the walls near a light and builds their shadow triangles. True when at least one wall casts a shadow from this light.</summary>
    private bool BuildShadows(Vector2 centre, float radius)
    {
        if (ShadowSources.Count == 0) return false;

        _edges.Clear();
        var reach = new Vector2(radius);
        foreach (var source in ShadowSources) source.CollectEdges(centre - reach, centre + reach, _edges);

        return ShadowMath.BuildShadowTriangles(centre, radius, _edges, _shadowTriangles) > 0;
    }

    /// <summary>
    /// Draws one light with its shadows. The sprites queued so far are flushed first (additive blending makes the order of lights irrelevant),
    /// then: clear the stencil, write 1 wherever a shadow shape covers (colour writes off), and draw the light only where the stencil is 0.
    /// </summary>
    private void DrawShadowed(SpriteDraw light)
    {
        _batch.End();

        _gl.Enable(EnableCap.StencilTest);
        _gl.StencilMask(0xFF);
        _gl.Clear(ClearBufferMask.StencilBufferBit);

        _gl.ColorMask(false, false, false, false);
        _gl.StencilFunc(StencilFunction.Always, 1, 0xFF);
        _gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
        _volumes.Draw(_shadowTriangles, _viewProjection);
        _gl.ColorMask(true, true, true, true);

        _gl.StencilFunc(StencilFunction.Equal, 0, 0xFF);
        _gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
        _batch.Begin(_viewProjection, SpriteBlend.Additive);
        _batch.Draw(_gradient, light);
        _batch.End();

        _gl.Disable(EnableCap.StencilTest);
        _batch.Begin(_viewProjection, SpriteBlend.Additive); // the lights after this one batch together again
        _shadowedLights++;
    }
}
