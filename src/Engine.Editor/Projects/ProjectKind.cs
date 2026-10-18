namespace Engine.Editor.Projects;

/// <summary>Which starting point a new project is made from.</summary>
public enum ProjectKind
{
    /// <summary>An empty 2D scene with a ground and a player square: the smallest project that runs.</summary>
    Empty2D,

    /// <summary>A 2D platformer: a tile level with collision, a hero that runs and jumps, a following camera.</summary>
    Platformer2D,

    /// <summary>A 3D scene: a lit floor and boxes under a procedural sky, viewed with a fly camera.</summary>
    Scene3D,
}
