namespace Engine.Physics3D;

/// <summary>
/// Answers one question: how far can a box move along an axis before it hits something solid? It sweeps the whole path, so a fast
/// box cannot tunnel through a thin wall. Obstacles are two lists of boxes: static solids, and the boxes of the other moving bodies.
/// Faces that merely touch do not block (see <see cref="Epsilon"/>). The 3D twin of <c>Engine.Physics2D.CollisionSolver</c>: the same
/// algorithm with a second perpendicular axis.
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

    /// <summary>
    /// The boxes of the moving bodies, one per body in the caller's own order. A body sweeping asks to ignore its own index, so it does not
    /// collide with itself. The caller keeps each entry up to date as its body moves.
    /// </summary>
    public List<Aabb3> DynamicBoxes { get; } = new();

    /// <summary>
    /// Oriented static solids (ramps, tilted platforms). Not part of the axis sweeps, which only know axis-aligned boxes: capsules are pushed
    /// out of them exactly, box bodies are pushed out of them after their move (see <c>PhysicsSystem3D</c>), and rays hit them exactly.
    /// </summary>
    public List<Obb3> StaticObbs { get; } = new();

    /// <summary>Finds how far <paramref name="box"/> can move along <paramref name="axis"/>, against the static solids only.</summary>
    /// <param name="box">The moving box at its start position.</param>
    /// <param name="axis">Axis to move along.</param>
    /// <param name="delta">Requested signed displacement along that axis.</param>
    /// <param name="hit">True when something solid stopped the box short of <paramref name="delta"/>.</param>
    /// <returns>The signed displacement actually allowed, between 0 and <paramref name="delta"/>.</returns>
    public float Sweep(in Aabb3 box, Axis3D axis, float delta, out bool hit)
    {
        float allowed = SweepCore(box, axis, delta, includeBodies: false, ignoreBody: -1, out var result);
        hit = result.Blocked;
        return allowed;
    }

    /// <summary>
    /// Finds how far <paramref name="box"/> can move along <paramref name="axis"/> against the static solids and the other moving bodies,
    /// and what stopped it (so the caller can pass momentum on to a body that was pushed).
    /// </summary>
    /// <param name="box">The moving box at its start position.</param>
    /// <param name="axis">Axis to move along.</param>
    /// <param name="delta">Requested signed displacement along that axis.</param>
    /// <param name="ignoreBody">Index in <see cref="DynamicBoxes"/> of the body that is moving (never an obstacle to itself), or -1.</param>
    /// <param name="hit">What blocked the box, if anything.</param>
    /// <returns>The signed displacement actually allowed, between 0 and <paramref name="delta"/>.</returns>
    public float Sweep(in Aabb3 box, Axis3D axis, float delta, int ignoreBody, out SweepHit hit) =>
        SweepCore(box, axis, delta, includeBodies: true, ignoreBody, out hit);

    private float SweepCore(in Aabb3 box, Axis3D axis, float delta, bool includeBodies, int ignoreBody, out SweepHit hit)
    {
        hit = SweepHit.None;
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

        for (int i = 0; i < StaticBoxes.Count; i++)
        {
            if (Blocks(StaticBoxes[i], axis, sign, front, first, second, firstLow, firstHigh, secondLow, secondHigh, ref limit))
                hit = new SweepHit(true, -1);
        }

        if (includeBodies)
        {
            for (int i = 0; i < DynamicBoxes.Count; i++)
            {
                if (i == ignoreBody) continue;
                if (Blocks(DynamicBoxes[i], axis, sign, front, first, second, firstLow, firstHigh, secondLow, secondHigh, ref limit))
                    hit = new SweepHit(true, i);
            }
        }

        return sign * limit;
    }

    /// <summary>Whether <paramref name="solid"/> is in the path and nearer than <paramref name="limit"/>; if so, lowers the limit to the gap.</summary>
    private static bool Blocks(
        in Aabb3 solid, Axis3D axis, float sign, float front, Axis3D first, Axis3D second,
        float firstLow, float firstHigh, float secondLow, float secondHigh, ref float limit)
    {
        if (solid.GetMax(first) <= firstLow || solid.GetMin(first) >= firstHigh) return false;
        if (solid.GetMax(second) <= secondLow || solid.GetMin(second) >= secondHigh) return false;

        // Distance from our leading face to the face of the obstacle that points back at us.
        float face = sign > 0f ? solid.GetMin(axis) : solid.GetMax(axis);
        float distance = sign * (face - front);
        if (distance < -Epsilon) return false; // behind us (already overlapping or passed)

        distance = MathF.Max(distance, 0f);
        if (distance >= limit) return false;

        limit = distance;
        return true;
    }
}
