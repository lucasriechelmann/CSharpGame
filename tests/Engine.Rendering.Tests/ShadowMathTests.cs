using System.Numerics;

namespace Engine.Rendering.Tests;

public class ShadowMathTests
{
    // A wall along the X axis with the solid below it (to its left when walking +X is up, so this wall has its solid ABOVE y=0).
    private static readonly ShadowEdge Floor = new(new Vector2(0, 0), new Vector2(10, 0));

    [Fact]
    public void The_outward_normal_is_to_the_right_of_the_walking_direction()
    {
        Assert.Equal(new Vector2(0, -1), Floor.OutwardNormal);
        Assert.Equal(new Vector2(1, 0), new ShadowEdge(new Vector2(0, 0), new Vector2(0, 5)).OutwardNormal);
        Assert.Equal(Vector2.Zero, new ShadowEdge(Vector2.One, Vector2.One).OutwardNormal);
    }

    [Fact]
    public void A_wall_casts_only_when_it_faces_away_from_the_light()
    {
        // Solid above the wall: its outward side is below. A light below faces it (lit); a light above is behind it (shadow).
        Assert.False(ShadowMath.FacesAwayFrom(new Vector2(5, -3), Floor));
        Assert.True(ShadowMath.FacesAwayFrom(new Vector2(5, 3), Floor));
    }

    [Fact]
    public void A_wall_is_in_range_when_any_part_of_it_is_within_the_radius()
    {
        Assert.True(ShadowMath.InRange(new Vector2(5, 4), 5f, Floor));   // directly above the middle, 4 away
        Assert.False(ShadowMath.InRange(new Vector2(5, 6), 5f, Floor));
        Assert.True(ShadowMath.InRange(new Vector2(13, 0), 4f, Floor));  // 3 past the end: the end point is the closest part
        Assert.False(ShadowMath.InRange(new Vector2(16, 0), 4f, Floor));
    }

    [Fact]
    public void Extruding_pushes_both_ends_away_along_the_rays_from_the_light()
    {
        Span<Vector2> quad = stackalloc Vector2[4];

        bool ok = ShadowMath.TryExtrude(new Vector2(5, 5), Floor, 10f, quad);

        Assert.True(ok);
        Assert.Equal(Floor.A, quad[0]);
        Assert.Equal(Floor.B, quad[1]);
        // Both ends are pushed 10 units straight away from the light at (5, 5).
        var pushedB = quad[2] - Floor.B;
        var pushedA = quad[3] - Floor.A;
        Assert.Equal(10f, pushedB.Length(), 3);
        Assert.Equal(10f, pushedA.Length(), 3);
        Assert.True(Vector2.Dot(pushedB, Floor.B - new Vector2(5, 5)) > 0f);
        Assert.True(quad[3].X < quad[2].X); // the shadow fans out away from the light
    }

    [Fact]
    public void A_wall_end_on_the_light_has_no_direction_so_it_is_skipped()
    {
        Span<Vector2> quad = stackalloc Vector2[4];

        Assert.False(ShadowMath.TryExtrude(Floor.A, Floor, 10f, quad));
    }

    [Fact]
    public void Building_triangles_keeps_only_in_range_back_facing_walls_two_triangles_each()
    {
        var edges = new List<ShadowEdge>
        {
            Floor,                                                      // behind it from a light above: casts
            new(new Vector2(10, 0), new Vector2(10, 5)),                // faces +X; light at x=5 is behind: casts
            new(new Vector2(10, 100), new Vector2(0, 100)),             // out of range
            new(new Vector2(0, -2), new Vector2(10, -2)),               // faces away? solid above y=-2, light above: back-facing, in range
        };
        var triangles = new List<Vector2>();

        int count = ShadowMath.BuildShadowTriangles(new Vector2(5, 3), 8f, edges, triangles);

        Assert.Equal(count, triangles.Count);
        Assert.Equal(0, count % 6);
        Assert.Equal(18, count); // three casting walls x 6 vertices
    }

    [Fact]
    public void A_box_lists_four_counter_clockwise_walls_that_face_outward()
    {
        var edges = new List<ShadowEdge>();

        BoxShadowCasterSource.AddBox(new Vector2(10, 20), new Vector2(2, 1), 0f, edges);

        Assert.Equal(4, edges.Count);
        Assert.Equal(new Vector2(8, 19), edges[0].A);
        Assert.Equal(new Vector2(0, -1), edges[0].OutwardNormal);  // bottom wall faces down
        Assert.Equal(new Vector2(1, 0), edges[1].OutwardNormal);   // right wall faces right
        Assert.Equal(new Vector2(0, 1), edges[2].OutwardNormal);   // top faces up
        Assert.Equal(new Vector2(-1, 0), edges[3].OutwardNormal);  // left faces left
    }

    [Fact]
    public void A_rotated_box_turns_its_walls()
    {
        var edges = new List<ShadowEdge>();

        BoxShadowCasterSource.AddBox(Vector2.Zero, new Vector2(2, 1), MathF.PI / 2f, edges);

        Assert.Equal(1f, edges[0].OutwardNormal.X, 4); // the bottom wall now faces +X after a quarter turn
        Assert.Equal(0f, edges[0].OutwardNormal.Y, 4);
    }
}
