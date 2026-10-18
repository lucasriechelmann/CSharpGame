using System.Numerics;
using Engine.Physics2D;

namespace Engine.Physics2D.Tests;

public class AabbTests
{
    [Fact]
    public void FromCenter_builds_the_expected_corners()
    {
        var box = Aabb.FromCenter(new Vector2(10, 20), new Vector2(4, 6));

        Assert.Equal(new Vector2(8, 17), box.Min);
        Assert.Equal(new Vector2(12, 23), box.Max);
        Assert.Equal(new Vector2(10, 20), box.Center);
        Assert.Equal(new Vector2(4, 6), box.Size);
    }

    [Fact]
    public void Overlapping_boxes_overlap()
    {
        var a = Aabb.FromCenter(Vector2.Zero, new Vector2(2, 2));
        var b = Aabb.FromCenter(new Vector2(1, 1), new Vector2(2, 2));

        Assert.True(a.Overlaps(b));
        Assert.True(b.Overlaps(a));
    }

    [Fact]
    public void Boxes_sharing_only_an_edge_do_not_overlap()
    {
        var a = Aabb.FromCenter(Vector2.Zero, new Vector2(2, 2));
        var right = Aabb.FromCenter(new Vector2(2, 0), new Vector2(2, 2));
        var above = Aabb.FromCenter(new Vector2(0, 2), new Vector2(2, 2));
        var corner = Aabb.FromCenter(new Vector2(2, 2), new Vector2(2, 2));

        Assert.False(a.Overlaps(right));
        Assert.False(a.Overlaps(above));
        Assert.False(a.Overlaps(corner));
    }

    [Fact]
    public void Separated_boxes_do_not_overlap()
    {
        var a = Aabb.FromCenter(Vector2.Zero, new Vector2(2, 2));
        var far = Aabb.FromCenter(new Vector2(10, 0), new Vector2(2, 2));

        Assert.False(a.Overlaps(far));
    }

    [Fact]
    public void Translated_moves_both_corners()
    {
        var box = new Aabb(new Vector2(1, 1), new Vector2(3, 4)).Translated(new Vector2(10, -1));

        Assert.Equal(new Vector2(11, 0), box.Min);
        Assert.Equal(new Vector2(13, 3), box.Max);
    }

    [Fact]
    public void Axis_accessors_pick_the_right_component()
    {
        var box = new Aabb(new Vector2(1, 2), new Vector2(3, 4));

        Assert.Equal(1f, box.GetMin(Axis.X));
        Assert.Equal(2f, box.GetMin(Axis.Y));
        Assert.Equal(3f, box.GetMax(Axis.X));
        Assert.Equal(4f, box.GetMax(Axis.Y));
    }
}
