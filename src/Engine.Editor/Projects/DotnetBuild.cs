using System.ComponentModel;
using System.Diagnostics;

namespace Engine.Editor.Projects;


/// <summary>
/// Runs the .NET SDK's <c>dotnet build</c> and <c>dotnet publish</c> for a game project. The editor itself is not a compiler: a game is ordinary C#,
/// so the SDK on the machine builds it, and the editor only tells it where the engine assemblies are (the <c>EngineDir</c> property, which a project's
/// <c>Directory.Build.props</c> turns into references).
/// </summary>
public static class DotnetBuild
{
    /// <summary>The folder holding the engine assemblies the editor itself runs on, with a trailing separator: what games are built against.</summary>
    public static string EngineDirectory => AppContext.BaseDirectory;

    /// <summary>The arguments of <c>dotnet build</c> for a project.</summary>
    /// <param name="project">Path to the <c>.csproj</c>.</param>
    /// <param name="configuration">Debug or Release.</param>
    /// <param name="engineDir">Folder holding the engine assemblies (with a trailing separator).</param>
    public static IReadOnlyList<string> BuildArguments(string project, string configuration, string engineDir) =>
        ["build", project, "-c", configuration, $"-p:EngineDir={engineDir}", "--nologo", "-v:minimal", "-clp:NoSummary"];

    /// <summary>The arguments of a self-contained <c>dotnet publish</c> for a project.</summary>
    /// <param name="project">Path to the <c>.csproj</c> of the program to ship.</param>
    /// <param name="runtime">The runtime identifier, such as <c>win-x64</c>.</param>
    /// <param name="output">The folder to publish into.</param>
    /// <param name="engineDir">Folder holding the engine assemblies (with a trailing separator).</param>
    public static IReadOnlyList<string> PublishArguments(string project, string runtime, string output, string engineDir) =>
        ["publish", project, "-c", "Release", "-r", runtime, "--self-contained", "-o", output, $"-p:EngineDir={engineDir}", "--nologo", "-v:minimal", "-clp:NoSummary"];

    /// <summary>Runs <c>dotnet</c> with the arguments and waits for it. Blocks, so call it off the UI thread.</summary>
    /// <param name="arguments">The arguments after <c>dotnet</c>.</param>
    /// <param name="onLine">Called for each line as it is printed (from a background thread).</param>
    public static BuildResult Run(IReadOnlyList<string> arguments, Action<string>? onLine = null)
    {
        var lines = new List<string>();
        void Record(string? line)
        {
            if (line is null) return;
            lock (lines) lines.Add(line);
            onLine?.Invoke(line);
        }

        var info = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (string argument in arguments) info.ArgumentList.Add(argument);

        try
        {
            using var process = new Process { StartInfo = info };
            process.OutputDataReceived += (_, e) => Record(e.Data);
            process.ErrorDataReceived += (_, e) => Record(e.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.WaitForExit(); // the second call waits for the output handlers to drain
            lock (lines) return new BuildResult(process.ExitCode == 0, process.ExitCode, lines.ToList());
        }
        catch (Win32Exception)
        {
            const string missing = "The .NET SDK ('dotnet') was not found on this machine. Install the .NET SDK to build games from the editor.";
            Record(missing);
            return new BuildResult(false, -1, [missing]);
        }
    }
}
