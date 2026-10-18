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
}
