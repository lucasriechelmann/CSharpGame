using System.Numerics;

namespace Engine.Physics3D;

/// <summary>Reads and writes a <see cref="Vector3"/> component by <see cref="Axis3D"/>, and names the two axes that are not the given one.</summary>
public static class AxisMath
{
    /// <summary>The component of <paramref name="v"/> along <paramref name="axis"/>.</summary>
    public static float Get(Vector3 v, Axis3D axis) => axis switch
    {
        Axis3D.X => v.X,
        Axis3D.Y => v.Y,
        _ => v.Z,
    };

    /// <summary>A vector that has <paramref name="value"/> on <paramref name="axis"/> and zero elsewhere.</summary>
    public static Vector3 Along(Axis3D axis, float value) => axis switch
    {
        Axis3D.X => new Vector3(value, 0f, 0f),
        Axis3D.Y => new Vector3(0f, value, 0f),
        _ => new Vector3(0f, 0f, value),
    };

    /// <summary>The two axes perpendicular to <paramref name="axis"/>, in X, Y, Z order.</summary>
    public static (Axis3D First, Axis3D Second) Others(Axis3D axis) => axis switch
    {
        Axis3D.X => (Axis3D.Y, Axis3D.Z),
        Axis3D.Y => (Axis3D.X, Axis3D.Z),
        _ => (Axis3D.X, Axis3D.Y),
    };
}
