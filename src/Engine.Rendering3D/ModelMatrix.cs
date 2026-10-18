using System.Numerics;
using Engine.Core.Components;

namespace Engine.Rendering3D;

/// <summary>Pure matrix helpers that turn an entity's <see cref="Transform"/> into what the mesh shader needs.</summary>
public static class ModelMatrix
{
    /// <summary>
    /// Local-to-world matrix: scale first, then rotate, then translate (row-vector order, so left to right is the order applied).
    /// </summary>
    public static Matrix4x4 From(in Transform transform) =>
        Matrix4x4.CreateScale(transform.Scale)
        * Matrix4x4.CreateFromQuaternion(transform.Rotation)
        * Matrix4x4.CreateTranslation(transform.Position);

    /// <summary>
    /// The matrix that carries a surface normal into world space: the inverse transpose of the model matrix. A plain model matrix
    /// would tilt normals the wrong way under non-uniform scale (squashing a sphere would make its lighting lean), because normals
    /// must stay perpendicular to the surface, not be scaled along with it. Row-vector form, so it is applied as <c>n * result</c>
    /// and uploaded without transposing, like every matrix here.
    /// </summary>
    /// <exception cref="InvalidOperationException">The model matrix is singular (a zero scale on some axis).</exception>
    public static Matrix4x4 NormalMatrix(in Matrix4x4 model)
    {
        if (!Matrix4x4.Invert(model, out var inverse))
            throw new InvalidOperationException("Model matrix is not invertible (does the transform have a zero scale?).");
        return Matrix4x4.Transpose(inverse);
    }

    /// <summary>The largest scale factor of the matrix, i.e. how much it can stretch a length; scales a bounding radius for culling.</summary>
    public static float MaxScale(in Matrix4x4 model)
    {
        float x = new Vector3(model.M11, model.M12, model.M13).Length();
        float y = new Vector3(model.M21, model.M22, model.M23).Length();
        float z = new Vector3(model.M31, model.M32, model.M33).Length();
        return MathF.Max(x, MathF.Max(y, z));
    }
}
