using System.Numerics;
using Engine.Core.Components;

namespace Engine.Core.Tests;

public class TransformTests
{
    [Fact]
    public void Identity_has_unit_scale_and_no_rotation()
    {
        var t = Transform.Identity;

        Assert.Equal(Vector3.One, t.Scale);
        Assert.Equal(Quaternion.Identity, t.Rotation);
        Assert.Equal(0f, t.RotationZ, 5);
    }

    [Theory]
    [InlineData(0.5f)]
    [InlineData(-1.25f)]
    [InlineData(3.0f)]
    public void Z_rotation_round_trips(float radians)
    {
        var t = Transform.Identity;
        t.SetRotationZ(radians);

        Assert.Equal(radians, t.RotationZ, 4);
    }

    [Fact]
    public void At_sets_position_and_keeps_identity_rotation_and_scale()
    {
        var t = Transform.At(1, 2, 3);

        Assert.Equal(new Vector3(1, 2, 3), t.Position);
        Assert.Equal(Vector3.One, t.Scale);
    }
}
