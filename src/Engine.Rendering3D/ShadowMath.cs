using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>The matrix that renders the scene from the sun, and how big one texel of the shadow map is in the world.</summary>
/// <param name="ViewProjection">World to the sun's clip space (row-vector, uploaded to GL untransposed).</param>
/// <param name="TexelWorldSize">World units covered by one shadow-map texel; shadows cannot be sharper than this.</param>
/// <param name="Centre">Centre of the fitted region in world space.</param>
/// <param name="Radius">Radius of the fitted sphere, in world units.</param>
public readonly record struct SunShadowFit(Matrix4x4 ViewProjection, float TexelWorldSize, Vector3 Centre, float Radius);

/// <summary>
/// Pure maths for sun shadow maps: where to point an orthographic camera so the shadow map covers what the player camera sees, as sharply as
/// the map's resolution allows, without the shadows crawling as the camera moves.
/// </summary>
public static class ShadowMath
{
    /// <summary>
    /// Fits an orthographic view along the sun's direction around the part of the camera's view frustum nearer than <paramref name="distance"/>.
    /// </summary>
    /// <remarks>
    /// Two tricks keep shadows steady. (1) The region is the <b>sphere</b> around the frustum slice, not its tight box: a sphere is the same
    /// size however the camera turns, so the map's texels do not change size and edges do not shimmer. (2) The region's centre is snapped to a
    /// whole texel in the sun's view, so as the camera moves a shadow edge keeps falling on the same texel boundaries instead of sliding
    /// across them. The depth range is stretched towards the sun by <paramref name="casterRange"/> so tall things outside the view can still
    /// cast into it.
    /// </remarks>
    /// <param name="camera">The viewing camera.</param>
    /// <param name="sunDirection">The direction the sunlight travels (unit length).</param>
    /// <param name="distance">How far from the camera shadows are wanted (clamped to the camera's far plane); smaller is sharper.</param>
    /// <param name="resolution">Side of the square shadow map in texels.</param>
    /// <param name="casterRange">How far towards the sun, past the fitted sphere, shadow casters are still drawn.</param>
    public static SunShadowFit FitToCamera(Camera3D camera, Vector3 sunDirection, float distance, int resolution, float casterRange = 120f)
    {
        if (resolution <= 0) throw new ArgumentOutOfRangeException(nameof(resolution), $"The shadow map needs a positive size, got {resolution}.");
        if (distance <= camera.Near) throw new ArgumentOutOfRangeException(nameof(distance), $"Shadow distance must be beyond the camera's near plane ({camera.Near}), got {distance}.");
        if (sunDirection.LengthSquared() < 1e-12f) throw new ArgumentException("The sun direction must not be zero.", nameof(sunDirection));

        sunDirection = Vector3.Normalize(sunDirection);
        float far = MathF.Min(distance, camera.Far);

        // The slice's eight corners in world space.
        Span<Vector3> corners = stackalloc Vector3[8];
        float tan = MathF.Tan(camera.FovY * 0.5f);
        int index = 0;
        foreach (float depth in stackalloc float[] { camera.Near, far })
        {
            float halfHeight = tan * depth;
            float halfWidth = halfHeight * camera.AspectRatio;
            var middle = camera.Position + camera.Forward * depth;
            foreach (float sx in stackalloc float[] { -1f, 1f })
            {
                foreach (float sy in stackalloc float[] { -1f, 1f })
                    corners[index++] = middle + camera.Right * (sx * halfWidth) + camera.Up * (sy * halfHeight);
            }
        }

        var centre = Vector3.Zero;
        foreach (var corner in corners) centre += corner;
        centre /= 8f;
        float radius = 0f;
        foreach (var corner in corners) radius = MathF.Max(radius, Vector3.Distance(corner, centre));
        radius = MathF.Ceiling(radius * 16f) / 16f; // a size that only changes in coarse steps, so the texel size is stable too

        var up = MathF.Abs(sunDirection.Y) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;
        var view = Matrix4x4.CreateLookTo(Vector3.Zero, sunDirection, up); // the sun's eye at the origin: only the orientation matters here
        float texel = 2f * radius / resolution;

        var centreInSunView = Vector3.Transform(centre, view);
        float snappedX = MathF.Round(centreInSunView.X / texel) * texel;
        float snappedY = MathF.Round(centreInSunView.Y / texel) * texel;

        // Depth in the sun's view is -Z. Things nearer the sun than the sphere are included up to casterRange.
        float nearPlane = -(centreInSunView.Z + radius) - casterRange;
        float farPlane = -(centreInSunView.Z - radius);
        var projection = Projection3D.Orthographic(snappedX - radius, snappedX + radius, snappedY - radius, snappedY + radius, nearPlane, farPlane);
        return new SunShadowFit(view * projection, texel, centre, radius);
    }
}
