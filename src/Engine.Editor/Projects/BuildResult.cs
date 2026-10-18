namespace Engine.Editor.Projects;

/// <summary>What a <c>dotnet</c> command did.</summary>
/// <param name="Success">True when it exited with code 0.</param>
/// <param name="ExitCode">The exit code, or -1 when the command could not be started.</param>
/// <param name="Lines">Everything it printed, line by line (errors included).</param>
public sealed record BuildResult(bool Success, int ExitCode, IReadOnlyList<string> Lines);
