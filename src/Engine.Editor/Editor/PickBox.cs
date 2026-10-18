using System.Numerics;

namespace Engine.Editor;

/// <summary>
/// An oriented box the editor can hit-test and outline: an axis-aligned box in some local space plus the matrix that places that space
/// in the world. A rotated or scaled mesh therefore gets a box that turns and stretches with it instead of a loose world-axis box.
/// </summary>
/// <param name="LocalToWorld">Row-vector matrix from the box's local space to world space.</param>
/// <param name="Min">Smallest corner in local space.</param>
/// <param name="Max">Largest corner in local space.</param>
public readonly record struct PickBox(Matrix4x4 LocalToWorld, Vector3 Min, Vector3 Max);
