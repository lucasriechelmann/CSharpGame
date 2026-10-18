using System.Diagnostics;
using Engine.Editor.Projects;
using ImGuiNET;

namespace Engine.Editor;

// The Project menu: build the game, reload the editor on new game code, publish a copy to ship, and check the assets.
internal sealed partial class SceneEditor
{
    private const string ConfirmReloadPopup = "Reload game code";

    private bool _openReloadConfirm;

    /// <summary>The project the editor has open; null when the editor was started on a bare game library (the Project menu is then absent).</summary>
    public ProjectSession? Project { get; init; }

    /// <summary>Call every frame: moves a running build's output into the log.</summary>
    private void PumpProject() => Project?.Update(line => _log.Info("Build", line));

    private void DrawProjectMenu()
    {
        if (Project is null || !ImGui.BeginMenu("Project")) return;

        bool free = !Project.Busy && !Playing;
        if (ImGui.MenuItem("Build", "Ctrl+B", false, free)) BuildProject();
        if (ImGui.MenuItem("Build and reload game code", null, false, free)) AskReload();
        if (ImGui.MenuItem("Validate assets", null, false, !Playing)) ValidateAssets();
        ImGui.Separator();
        if (ImGui.MenuItem("Publish game...", null, false, free && Project.Project.Launcher.Length > 0)) PublishProject();
        ImGui.Separator();
        if (ImGui.MenuItem("Open another project...")) StartAnotherEditor(args: []);
        if (ImGui.MenuItem("Open project folder")) OpenInExplorer(Project.Project.Root);
        ImGui.EndMenu();
    }

    /// <summary>Compiles the game library in the background. New game code only takes effect after <see cref="AskReload"/>.</summary>
    private void BuildProject()
    {
        if (Project is null) return;

        var args = DotnetBuild.BuildArguments(Project.Project.GameProjectPath, "Debug", DotnetBuild.EngineDirectory);
        if (!Project.Start("Building", args, result => _log.Info("Build", result.Success ? "Build succeeded." : "Build FAILED, see the lines above.")))
            return;

        _log.Info("Build", $"Building {Project.Project.Name}...");
    }

    /// <summary>Asks to save first when the scene has unsaved edits, then builds and restarts the editor on the same project.</summary>
    private void AskReload()
    {
        if (_history.CanUndo) _openReloadConfirm = true; // unsaved edits would be lost by the restart
        else BuildAndReload();
    }

    private void BuildAndReload()
    {
        if (Project is null) return;

        var args = DotnetBuild.BuildArguments(Project.Project.GameProjectPath, "Debug", DotnetBuild.EngineDirectory);
        if (!Project.Start("Building", args, result =>
            {
                if (!result.Success)
                {
                    _log.Error("Build", "Build FAILED: the editor keeps the game code it has.");
                    return;
                }

                _log.Info("Build", "Build succeeded; restarting the editor on the new game code.");
                StartAnotherEditor([ProjectFilePath(), "--no-build"]);
                _game.Quit();
            }))
            return;

        _log.Info("Build", $"Building {Project.Project.Name}...");
    }

    private void PublishProject()
    {
        if (Project is null) return;

        string output = Path.Combine(Project.Project.Root, "Build", "win-x64");
        var args = DotnetBuild.PublishArguments(Project.Project.LauncherPath, "win-x64", output, DotnetBuild.EngineDirectory);
        if (!Project.Start("Publishing", args, result =>
            {
                _log.Info("Build", result.Success ? $"Published to {output}" : "Publish FAILED, see the lines above.");
                if (result.Success) OpenInExplorer(output);
            }))
            return;

        _log.Info("Build", $"Publishing {Project.Project.Name} to {output}...");
    }

    /// <summary>Loads every asset the way the game would and reports the ones that fail.</summary>
    private void ValidateAssets()
    {
        var problems = AssetValidator.Validate(_assets.RootDirectory, _assets, _game.Components, out int checkedCount);
        foreach (var problem in problems) _log.Error("Assets", $"{problem.Asset}: {problem.Message}");
        _log.Info("Assets", problems.Count == 0
            ? $"All {checkedCount} assets load."
            : $"{problems.Count} of {checkedCount} assets have problems.");
    }

    /// <summary>The save-before-reload question. Draw it every frame, outside any menu.</summary>
    private void DrawReloadConfirm()
    {
        if (_openReloadConfirm)
        {
            ImGui.OpenPopup(ConfirmReloadPopup);
            _openReloadConfirm = false;
        }

        if (!ImGui.BeginPopupModal(ConfirmReloadPopup, ImGuiWindowFlags.AlwaysAutoResize)) return;

        ImGui.TextUnformatted("The scene has changes that are not saved.");
        ImGui.TextDisabled("Restarting the editor to load new game code would lose them.");
        ImGui.Dummy(new System.Numerics.Vector2(0f, 6f));
        if (ImGui.Button("Save and reload"))
        {
            SaveScene();
            ImGui.CloseCurrentPopup();
            BuildAndReload();
        }

        ImGui.SameLine();
        if (ImGui.Button("Reload without saving"))
        {
            ImGui.CloseCurrentPopup();
            BuildAndReload();
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private string ProjectFilePath() => Directory.EnumerateFiles(Project!.Project.Root, "*" + ProjectFile.Extension).First();

    private static void StartAnotherEditor(string[] args)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
        foreach (string arg in args) start.ArgumentList.Add(arg);
        Process.Start(start);
    }

    private static void OpenInExplorer(string folder) => Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
}
