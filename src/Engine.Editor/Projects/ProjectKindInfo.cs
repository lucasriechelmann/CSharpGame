namespace Engine.Editor.Projects;

/// <summary>Names and descriptions of the <see cref="ProjectKind"/> values, for the hub's template picker and the command line.</summary>
public static class ProjectKindInfo
{
    /// <summary>The name shown in the hub.</summary>
    public static string Title(ProjectKind kind) => kind switch
    {
        ProjectKind.Empty2D => "Empty 2D",
        ProjectKind.Platformer2D => "2D platformer",
        ProjectKind.Scene3D => "3D scene",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown project kind."),
    };

    /// <summary>One line saying what the project starts with.</summary>
    public static string Description(ProjectKind kind) => kind switch
    {
        ProjectKind.Empty2D => "A ground and a player square. Add your own game logic.",
        ProjectKind.Platformer2D => "A tile level with collision and a hero that runs and jumps (A/D, Space).",
        ProjectKind.Scene3D => "A lit floor and boxes under a procedural sky; fly with W/A/S/D, Q/E and the right mouse button.",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown project kind."),
    };

    /// <summary>The word used for the kind on the command line (<c>--template platformer</c>).</summary>
    public static string Keyword(ProjectKind kind) => kind switch
    {
        ProjectKind.Empty2D => "empty",
        ProjectKind.Platformer2D => "platformer",
        ProjectKind.Scene3D => "3d",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown project kind."),
    };

    /// <summary>Finds the kind a command-line word names.</summary>
    /// <param name="keyword">The word, any case.</param>
    /// <param name="kind">The kind it names.</param>
    public static bool TryParse(string keyword, out ProjectKind kind)
    {
        foreach (var candidate in Enum.GetValues<ProjectKind>())
        {
            if (!string.Equals(Keyword(candidate), keyword, StringComparison.OrdinalIgnoreCase)) continue;

            kind = candidate;
            return true;
        }

        kind = ProjectKind.Empty2D;
        return false;
    }
}
