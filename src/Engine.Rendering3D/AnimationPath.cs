namespace Engine.Rendering3D;

/// <summary>Which part of a node's transform an animation channel drives.</summary>
public enum AnimationPath
{
    /// <summary>The node's position relative to its parent (3 numbers per key).</summary>
    Translation,

    /// <summary>The node's orientation (a quaternion, 4 numbers per key).</summary>
    Rotation,

    /// <summary>The node's scale (3 numbers per key).</summary>
    Scale,
}
