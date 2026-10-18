namespace Engine.Physics2D;

// Obstacles that are cells of the tile grid.
public sealed partial class CollisionSolver
{
    private static float SweepTiles(TileGrid grid, in Aabb box, Axis axis, float sign, float limit, ref bool hit)
    {
        var other = Axis.X == axis ? Axis.Y : Axis.X;
        float front = Front(box, axis, sign);
        float tileSize = grid.TileSize;
        float origin = grid.OriginAlong(axis);

        // Cells the box spans on the other axis (shrunk by epsilon so touching cells do not count).
        int acrossFrom = Math.Max(grid.CellIndex(box.GetMin(other) + Epsilon, other), 0);
        int acrossTo = Math.Min(grid.CellIndex(box.GetMax(other) - Epsilon, other), grid.CellCount(other) - 1);

        // Columns (or rows) along the path, nearest first, clamped to the grid since outside is empty.
        int last = grid.CellCount(axis) - 1;
        int start, end, step;
        if (sign > 0f)
        {
            start = Math.Max(grid.CellIndex(front - Epsilon, axis), 0);
            end = Math.Min(grid.CellIndex(front + limit, axis), last);
            step = 1;
        }
        else
        {
            start = Math.Min(grid.CellIndex(front + Epsilon, axis), last);
            end = Math.Max(grid.CellIndex(front - limit, axis), 0);
            step = -1;
        }

        for (int along = start; sign > 0f ? along <= end : along >= end; along += step)
        {
            // Face of this column/row that points back at the box.
            float face = sign > 0f ? origin + along * tileSize : origin + (along + 1) * tileSize;
            float distance = sign * (face - front);
            if (distance < -Epsilon) continue; // behind the leading edge

            distance = MathF.Max(distance, 0f);
            if (distance >= limit) break; // every later column is farther away

            for (int across = acrossFrom; across <= acrossTo; across++)
            {
                bool solid = axis == Axis.X ? grid.IsSolid(along, across) : grid.IsSolid(across, along);
                if (!solid) continue;

                limit = distance;
                hit = true;
                return limit; // nearest column found; nothing farther can be closer
            }
        }

        return limit;
    }
}
