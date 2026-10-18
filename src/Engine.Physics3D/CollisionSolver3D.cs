namespace Engine.Physics3D;

/// <summary>
/// Answers one question: how far can a box move along an axis before it hits something solid? It sweeps the whole path, so a fast
/// box cannot tunnel through a thin wall. Solids are a list of static boxes. Faces that merely touch do not block (see
/// <see cref="Epsilon"/>). The 3D twin of <c>Engine.Physics2D.CollisionSolver</c>: the same algorithm with a second perpendicular axis.
/// </summary>
/// <remarks>
/// This file holds the sweep; <c>CollisionSolver3D.Raycast.cs</c> holds the ray queries.
/// </remarks>
public sealed partial class CollisionSolver3D
{
    /// <summary>
    /// Tolerance for float error. Overlap on the perpendicular axes must exceed it to count, and obstacles up to this far "behind" the
    /// leading face are still treated as ahead, so resting contact is stable.
    /// </summary>
    public const float Epsilon = 0.001f;

    /// <summary>Static solid boxes (floors, walls, crates) to collide with. Refill it each step.</summary>
    public List<Aabb3> StaticBoxes { get; } = new();

    /// <summary>Finds how far <paramref name="box"/> can move along <paramref name="axis"/>.</summary>
    /// <param name="box">The moving box at its start position.</param>
    /// <param name="axis">Axis to move along.</param>
    /// <param name="delta">Requested signed displacement along that axis.</param>
    /// <param name="hit">True when something solid stopped the box short of <paramref name="delta"/>.</param>
    /// <returns>The signed displacement actually allowed, between 0 and <paramref name="delta"/>.</returns>
    public float Sweep(in Aabb3 box, Axis3D axis, float delta, out bool hit)
    {
        hit = false;
        if (delta == 0f) return 0f;

        float sign = MathF.Sign(delta);
        float limit = MathF.Abs(delta);
        var (first, second) = AxisMath.Others(axis);

        float front = sign > 0f ? box.GetMax(axis) : box.GetMin(axis);
        // The obstacle must overlap the box's extent on both other axes (shrunk by Epsilon so touching is not overlapping); otherwise it
        // is beside the path, not in it.
        float firstLow = box.GetMin(first) + Epsilon;
        float firstHigh = box.GetMax(first) - Epsilon;
        float secondLow = box.GetMin(second) + Epsilon;
        float secondHigh = box.GetMax(second) - Epsilon;

        foreach (var solid in StaticBoxes)
        {
            if (solid.GetMax(first) <= firstLow || solid.GetMin(first) >= firstHigh) continue;
            if (solid.GetMax(second) <= secondLow || solid.GetMin(second) >= secondHigh) continue;

            // Distance from our leading face to the face of the obstacle that points back at us.
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

        return sign * limit;
    }
}
