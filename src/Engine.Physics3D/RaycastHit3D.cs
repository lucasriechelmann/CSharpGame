using System.Numerics;

namespace Engine.Physics3D;

/// <summary>Where a ray first met a solid box.</summary>
/// <param name="Distance">How far along the ray the hit is.</param>
/// <param name="Point">The hit position in world space.</param>
/// <param name="Normal">The unit normal of the face that was hit (pointing back toward the ray); the reverse of the ray direction when it started inside the box.</param>
/// <param name="Index">Index of the box in <see cref="CollisionSolver3D.StaticBoxes"/>.</param>
public readonly record struct RaycastHit3D(float Distance, Vector3 Point, Vector3 Normal, int Index);
