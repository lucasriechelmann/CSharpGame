using System.Numerics;
using Engine.Physics3D;

namespace Engine.Physics3D.Tests;

public class Aabb3Tests
{
    [Fact]
    public void FromCenter_builds_the_box_around_the_centre()
    {
        var box = Aabb3.FromCenter(new Vector3(1f, 2f, 3f), new Vector3(2f, 4f, 6f));

        Assert.Equal(new Vector3(0f, 0f, 0f), box.Min);
        Assert.Equal(new Vector3(2f, 4f, 6f), box.Max);
        Assert.Equal(new Vector3(1f, 2f, 3f), box.Center);
        Assert.Equal(new Vector3(2f, 4f, 6f), box.Size);
    }

    [Fact]
    public void Boxes_that_share_a_face_edge_or_corner_do_not_overlap()
    {
        var a = Aabb3.FromCenter(Vector3.Zero, Vector3.One);

        Assert.False(a.Overlaps(a.Translated(new Vector3(1f, 0f, 0f)))); // face
        Assert.False(a.Overlaps(a.Translated(new Vector3(1f, 1f, 0f)))); // edge
        Assert.False(a.Overlaps(a.Translated(new Vector3(1f, 1f, 1f)))); // corner
    }

    [Fact]
    public void Boxes_overlap_when_interiors_cross_on_every_axis()
    {
        var a = Aabb3.FromCenter(Vector3.Zero, Vector3.One);

        Assert.True(a.Overlaps(a.Translated(new Vector3(0.5f, 0.5f, 0.5f))));
        Assert.False(a.Overlaps(a.Translated(new Vector3(0.5f, 0.5f, 2f)))); // overlaps on X and Y only
    }

    [Fact]
    public void Axis_accessors_pick_the_matching_component()
    {
        var box = new Aabb3(new Vector3(1f, 2f, 3f), new Vector3(4f, 5f, 6f));

        Assert.Equal(2f, box.GetMin(Axis3D.Y));
        Assert.Equal(6f, box.GetMax(Axis3D.Z));
    }

    [Fact]
    public void The_other_two_axes_are_the_ones_left()
    {
        Assert.Equal((Axis3D.Y, Axis3D.Z), AxisMath.Others(Axis3D.X));
        Assert.Equal((Axis3D.X, Axis3D.Z), AxisMath.Others(Axis3D.Y));
        Assert.Equal((Axis3D.X, Axis3D.Y), AxisMath.Others(Axis3D.Z));
        Assert.Equal(new Vector3(0f, 0f, 7f), AxisMath.Along(Axis3D.Z, 7f));
    }
}
