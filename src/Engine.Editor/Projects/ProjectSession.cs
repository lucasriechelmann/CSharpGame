using System.Collections.Concurrent;

namespace Engine.Editor.Projects;

/// <summary>
/// The project the editor has open, and the one background job it may be running for it (a build or a publish). The job runs on a worker thread
/// and its output is collected in a queue; the editor calls <see cref="Update"/> every frame to move that output to the log and to learn when the
/// job has finished, so the interface never blocks while <c>dotnet</c> works.
/// </summary>
public sealed class ProjectSession(ProjectFile project)
{
    private readonly ConcurrentQueue<string> _output = new();
    private Task<BuildResult>? _job;
    private Action<BuildResult>? _whenDone;

    /// <summary>The open project.</summary>
    public ProjectFile Project { get; } = project;

    /// <summary>True while a build or publish is running.</summary>
    public bool Busy => _job != null;

    /// <summary>What the running job is, such as "Building"; empty when idle.</summary>
    public string Activity { get; private set; } = string.Empty;

    /// <summary>Starts a job. Does nothing and returns false when one is already running.</summary>
    /// <param name="activity">What to call it while it runs, such as "Building".</param>
    /// <param name="arguments">The <c>dotnet</c> arguments.</param>
    /// <param name="whenDone">Called from <see cref="Update"/> (on the editor's thread) once the job has finished.</param>
    public bool Start(string activity, IReadOnlyList<string> arguments, Action<BuildResult> whenDone)
    {
        if (Busy) return false;

        Activity = activity;
        _whenDone = whenDone;
        _job = Task.Run(() => DotnetBuild.Run(arguments, _output.Enqueue));
        return true;
    }

    /// <summary>Call every frame from the editor's thread: passes new output lines to <paramref name="log"/> and, when the job finished, runs its completion callback.</summary>
    /// <param name="log">Receives each non-empty output line.</param>
    public void Update(Action<string> log)
    {
        while (_output.TryDequeue(out string? line))
        {
            if (!string.IsNullOrWhiteSpace(line)) log(line);
        }

        if (_job is not { IsCompleted: true } finished) return;

        var callback = _whenDone;
        _job = null;
        _whenDone = null;
        Activity = string.Empty;
        callback?.Invoke(finished.Result);
    }
}
