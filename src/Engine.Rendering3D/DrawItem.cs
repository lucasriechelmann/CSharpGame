using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>One mesh to draw this frame: what, with which material, where, and the numbers culling and sorting need.</summary>
internal struct DrawItem
{
    /// <summary>The geometry.</summary>
    public Mesh Mesh;

    /// <summary>The surface look.</summary>
    public Material Material;

    /// <summary>Local-to-world matrix.</summary>
    public Matrix4x4 Model;

    /// <summary>Centre of the bounding sphere in world space.</summary>
    public Vector3 Centre;

    /// <summary>Radius of the bounding sphere in world space.</summary>
    public float Radius;

    /// <summary>The joint matrices to deform a skinned mesh by (this frame's pose of its skeleton), or null for a rigid mesh. Skinned items are never instanced: each has its own pose.</summary>
    public Matrix4x4[]? Joints;

    /// <summary>Squared distance from the camera to <see cref="Centre"/>: blended items are drawn farthest first.</summary>
    public float DistanceSquared;
}
