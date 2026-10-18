namespace Engine.Physics3D;

/// <summary>What stopped a box during <see cref="CollisionSolver3D.Sweep(in Aabb3, Axis3D, float, int, out SweepHit)"/>.</summary>
/// <param name="Blocked">True when something solid stopped the box short of the requested distance.</param>
/// <param name="DynamicIndex">Index into <see cref="CollisionSolver3D.DynamicBoxes"/> of the moving body that blocked it, or -1 when the obstacle was a static solid (or nothing blocked).</param>
public readonly record struct SweepHit(bool Blocked, int DynamicIndex)
{
    /// <summary>Nothing blocked the sweep.</summary>
    public static SweepHit None => new(false, -1);

    /// <summary>True when the obstacle was another moving body.</summary>
    public bool IsBody => DynamicIndex >= 0;
}
