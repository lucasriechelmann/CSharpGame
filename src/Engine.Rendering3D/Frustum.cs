using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>
/// The volume a camera can see, as six inward-facing planes (left, right, bottom, top, near, far). Used to skip drawing meshes
/// that are entirely outside the view.
/// </summary>
public readonly struct Frustum
{
    private readonly Plane[] _planes;

    private Frustum(Plane[] planes) => _planes = planes;

    /// <summary>
    /// Extracts the planes from a view-projection matrix (Gribb/Hartmann). A clip-space point is inside when
    /// <c>-w &lt;= x, y, z &lt;= w</c>; each inequality is a plane that is a sum or difference of two matrix columns (columns because
    /// <see cref="Matrix4x4"/> multiplies row vectors on the left).
    /// </summary>
    /// <param name="viewProjection">World-to-clip matrix using OpenGL's -1..1 depth range (see <see cref="Projection3D.Perspective"/>).</param>
    public static Frustum FromViewProjection(in Matrix4x4 viewProjection)
    {
        var m = viewProjection;
        var column1 = new Vector4(m.M11, m.M21, m.M31, m.M41);
        var column2 = new Vector4(m.M12, m.M22, m.M32, m.M42);
        var column3 = new Vector4(m.M13, m.M23, m.M33, m.M43);
        var column4 = new Vector4(m.M14, m.M24, m.M34, m.M44);

        return new Frustum(
        [
            Normalised(column4 + column1), // left:   w + x >= 0
            Normalised(column4 - column1), // right:  w - x >= 0
            Normalised(column4 + column2), // bottom: w + y >= 0
            Normalised(column4 - column2), // top:    w - y >= 0
            Normalised(column4 + column3), // near:   w + z >= 0
            Normalised(column4 - column3), // far:    w - z >= 0
        ]);
    }

    /// <summary>False only when the sphere is completely outside at least one plane; a sphere crossing the edge counts as visible.</summary>
    /// <param name="centre">Sphere centre in world space.</param>
    /// <param name="radius">Sphere radius in world units.</param>
    public bool IntersectsSphere(Vector3 centre, float radius)
    {
        foreach (var plane in _planes)
            if (Plane.DotCoordinate(plane, centre) < -radius) return false;
        return true;
    }

    private static Plane Normalised(Vector4 v) => Plane.Normalize(new Plane(v.X, v.Y, v.Z, v.W));
}
