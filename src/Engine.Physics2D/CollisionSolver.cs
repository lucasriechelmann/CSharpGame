namespace Engine.Physics2D;

/// <summary>
/// Answers one question: how far can a box move along an axis before it hits something solid? It sweeps the
/// whole path, so a fast box cannot tunnel through a thin wall. Solids are a <see cref="TileGrid"/> plus a list
/// of static boxes. Edges that merely touch do not block (see <see cref="Epsilon"/>).
/// </summary>
/// <remarks>
/// This file holds the public entry point; <c>CollisionSolver.Tiles.cs</c> and <c>CollisionSolver.Boxes.cs</c> hold the
/// two kinds of obstacle.
/// </remarks>
/// <param name="grid">Tile collision, or null when the level has no tiles.</param>
public sealed partial class CollisionSolver(TileGrid? grid)
{
    /// <summary>
    /// Tolerance for float error. Perpendicular overlap must exceed it to count, and obstacles up to this far
    /// "behind" the leading edge are still treated as ahead, so resting contact is stable.
    /// </summary>
    public const float Epsilon = 0.001f;

    /// <summary>
    /// The tile collision, or null for none. Settable so the level can be rebuilt (the editor changes tiles) without replacing the
    /// systems that hold this solver.
    /// </summary>
    public TileGrid? Grid { get; set; } = grid;

    /// <summary>Static solid boxes (platforms, crates) checked in addition to the tile grid. Refill it each step.</summary>
    public List<Aabb> StaticBoxes { get; } = new();

    /// <summary>
    /// Finds how far <paramref name="box"/> can move along <paramref name="axis"/>.
    /// </summary>
    /// <param name="box">The moving box at its start position.</param>
    /// <param name="axis">Axis to move along.</param>
    /// <param name="delta">Requested signed displacement along that axis.</param>
    /// <param name="hit">True when something solid stopped the box short of <paramref name="delta"/>.</param>
    /// <returns>The signed displacement actually allowed, between 0 and <paramref name="delta"/>.</returns>
    public float Sweep(in Aabb box, Axis axis, float delta, out bool hit)
    {
        hit = false;
        if (delta == 0f) return 0f;

        float sign = MathF.Sign(delta);
        float limit = MathF.Abs(delta);

        limit = SweepBoxes(box, axis, sign, limit, ref hit);
        if (Grid is not null) limit = SweepTiles(Grid, box, axis, sign, limit, ref hit);

        return sign * limit;
    }

    private static Axis Other(Axis axis) => axis == Axis.X ? Axis.Y : Axis.X;

    /// <summary>Coordinate of the box's leading edge when moving in direction <paramref name="sign"/>.</summary>
    private static float Front(in Aabb box, Axis axis, float sign) => sign > 0f ? box.GetMax(axis) : box.GetMin(axis);
}
