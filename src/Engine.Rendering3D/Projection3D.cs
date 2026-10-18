using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>Projection matrices written for OpenGL's clip-space conventions.</summary>
public static class Projection3D
{
    /// <summary>
    /// A perspective projection for a camera looking down -Z that maps depth between the near and far planes to the clip range
    /// -1..1 (OpenGL's convention).
    /// </summary>
    /// <remarks>
    /// Why not <see cref="Matrix4x4.CreatePerspectiveFieldOfView"/>: it targets Direct3D and maps depth to 0..1. OpenGL 3.3 clips
    /// at -1, so that matrix would leave the near plane inside the volume (geometry in front of it would not be clipped) and
    /// waste half the depth precision. Row-vector form: a point is multiplied on the left, <c>clip = eye * M</c>.
    /// </remarks>
    /// <param name="fovY">Vertical field of view in radians.</param>
    /// <param name="aspect">Width divided by height.</param>
    /// <param name="near">Near plane distance (positive).</param>
    /// <param name="far">Far plane distance (greater than near).</param>
    public static Matrix4x4 Perspective(float fovY, float aspect, float near, float far)
    {
        if (fovY <= 0f || fovY >= MathF.PI) throw new ArgumentOutOfRangeException(nameof(fovY), $"Field of view must be between 0 and pi, got {fovY}.");
        if (aspect <= 0f) throw new ArgumentOutOfRangeException(nameof(aspect), $"Aspect ratio must be positive, got {aspect}.");
        if (near <= 0f || far <= near) throw new ArgumentOutOfRangeException(nameof(near), $"Need 0 < near < far, got near={near}, far={far}.");

        float f = 1f / MathF.Tan(fovY * 0.5f);
        float range = near - far;
        return new Matrix4x4(
            f / aspect, 0f, 0f, 0f,
            0f, f, 0f, 0f,
            0f, 0f, (far + near) / range, -1f,
            0f, 0f, 2f * far * near / range, 0f);
    }

    /// <summary>
    /// An orthographic projection for a view looking down -Z, mapping depth to OpenGL's clip range -1..1. Parallel lines stay parallel (no
    /// perspective), which is what a directional light's shadow map wants. Row-vector form like <see cref="Perspective"/>.
    /// </summary>
    /// <param name="left">Left edge of the view volume in view space.</param>
    /// <param name="right">Right edge.</param>
    /// <param name="bottom">Bottom edge.</param>
    /// <param name="top">Top edge.</param>
    /// <param name="near">Distance of the near plane in front of the eye along -Z (may be negative to include things behind the eye).</param>
    /// <param name="far">Distance of the far plane, greater than <paramref name="near"/>.</param>
    public static Matrix4x4 Orthographic(float left, float right, float bottom, float top, float near, float far)
    {
        if (right <= left) throw new ArgumentOutOfRangeException(nameof(right), $"Need left < right, got {left} and {right}.");
        if (top <= bottom) throw new ArgumentOutOfRangeException(nameof(top), $"Need bottom < top, got {bottom} and {top}.");
        if (far <= near) throw new ArgumentOutOfRangeException(nameof(far), $"Need near < far, got near={near}, far={far}.");
        return new Matrix4x4(
            2f / (right - left), 0f, 0f, 0f,
            0f, 2f / (top - bottom), 0f, 0f,
            0f, 0f, -2f / (far - near), 0f,
            -(right + left) / (right - left), -(top + bottom) / (top - bottom), -(far + near) / (far - near), 1f);
    }
}
