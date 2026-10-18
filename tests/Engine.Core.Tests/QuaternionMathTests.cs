using System.Numerics;

namespace Engine.Core.Tests;

public class QuaternionMathTests
{
    [Theory]
    [InlineData(0f, 0f, 0f)]
    [InlineData(0f, 0f, 90f)]
    [InlineData(0f, 45f, 0f)]
    [InlineData(30f, 0f, 0f)]
    [InlineData(20f, 40f, 60f)]
    [InlineData(-25f, -70f, 130f)]
    public void Euler_degrees_round_trip(float pitch, float yaw, float roll)
    {
        var euler = new Vector3(pitch, yaw, roll);

        var back = QuaternionMath.ToEulerDegrees(QuaternionMath.FromEulerDegrees(euler));

        Assert.Equal(pitch, back.X, 2);
        Assert.Equal(yaw, back.Y, 2);
        Assert.Equal(roll, back.Z, 2);
    }

    [Fact]
    public void Identity_has_no_rotation()
    {
        var euler = QuaternionMath.ToEulerDegrees(Quaternion.Identity);

        Assert.Equal(Vector3.Zero, euler);
    }

    [Fact]
    public void Roll_is_the_rotation_about_Z_used_by_2D()
    {
        var q = QuaternionMath.FromEulerDegrees(new Vector3(0f, 0f, 90f));

        var rotated = Vector3.Transform(Vector3.UnitX, q);

        Assert.Equal(0f, rotated.X, 4);
        Assert.Equal(1f, rotated.Y, 4);
    }

    [Fact]
    public void Pitch_is_clamped_instead_of_producing_NaN_at_the_pole()
    {
        var q = QuaternionMath.FromEulerDegrees(new Vector3(90f, 0f, 0f));

        var euler = QuaternionMath.ToEulerDegrees(q);

        Assert.False(float.IsNaN(euler.X));
        Assert.Equal(90f, euler.X, 1);
    }
}
