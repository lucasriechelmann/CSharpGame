namespace Engine.Physics2D;

// Obstacles that are plain boxes.
public sealed partial class CollisionSolver
{
    private float SweepBoxes(in Aabb box, Axis axis, float sign, float limit, ref bool hit)
    {
        var other = Other(axis);
        float front = Front(box, axis, sign);
        float acrossLow = box.GetMin(other) + Epsilon;
        float acrossHigh = box.GetMax(other) - Epsilon;

        foreach (var solid in StaticBoxes)
        {
            // Must overlap the box's extent on the other axis, otherwise it is beside the path, not in it.
            if (solid.GetMax(other) <= acrossLow || solid.GetMin(other) >= acrossHigh) continue;

            // Distance from our leading edge to the face of the obstacle that points back at us.
            float face = sign > 0f ? solid.GetMin(axis) : solid.GetMax(axis);
            float distance = sign * (face - front);
            if (distance < -Epsilon) continue; // behind us (already overlapping or passed)

            distance = MathF.Max(distance, 0f);
            if (distance < limit)
            {
                limit = distance;
                hit = true;
            }
        }

        return limit;
    }
}
