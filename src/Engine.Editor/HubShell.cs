using System.Numerics;
using Engine.Editor.Projects;
using Engine.Editor.Theme;
using Engine.Runtime;
using ImGuiNET;

namespace Engine.Editor;

/// <summary>
/// The project hub, shown when the editor starts without a project: start a new game, open an existing project, or pick one from the recent list.
/// Opening builds the game (so the editor always shows current code) and then starts a second editor process on the project; the hub closes. This
/// file is the interface; <c>HubShell.Projects.cs</c> does the work behind the buttons.
/// </summary>
internal sealed partial class HubShell : IEditorShell
{
    private enum Page
    {
        Home,
        NewProject,
    }

    private const float RowHeight = 54f;

    private readonly Game _game;
    private readonly RecentProjects _recent;
    private Page _page = Page.Home;
    private bool _overlayHidden;

    /// <summary>Creates the hub.</summary>
    /// <param name="game">The hub's own game, which provides the window.</param>
    /// <param name="recent">The recent project list.</param>
    public HubShell(Game game, RecentProjects recent)
    {
        _game = game;
        _recent = recent;
        _location = DefaultLocation();
    }

    /// <inheritdoc />
    public bool Active => true;

    /// <inheritdoc />
    public bool Editing => true;

    /// <inheritdoc />
    public void Toggle()
    {
    }

    /// <inheritdoc />
    public void BeginFrame()
    {
        if (_overlayHidden) return;

        _overlayHidden = true;
        _game.HideDebugOverlay(); // the hub has its own interface, not the F1 debug windows
    }

    /// <inheritdoc />
    public void Draw()
    {
        PumpBuild();

        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.WorkPos);
        ImGui.SetNextWindowSize(viewport.WorkSize);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(48f, 36f));
        ImGui.Begin("##hub", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoBringToFrontOnFocus);
        ImGui.PopStyleVar(3);

        ImGui.SetWindowFontScale(1.7f);
        ImGui.TextUnformatted("Game Editor");
        ImGui.SetWindowFontScale(1f);
        ImGui.TextDisabled("Make a game, or open one you have been working on.");
        ImGui.Dummy(new Vector2(0f, 10f));

        if (Building) DrawBuild();
        else if (_page == Page.NewProject) DrawNewProject();
        else DrawHome();

        ImGui.End();
    }

    private void DrawHome()
    {
        if (AccentButton(Icons.Add + "  New project")) OpenNewProjectPage();
        ImGui.SameLine();
        if (ImGui.Button(Icons.FolderOpen + "  Open project...")) BrowseForProject();

        if (_message.Length > 0)
        {
            ImGui.Dummy(new Vector2(0f, 6f));
            Warning(_message);
        }

        ImGui.Dummy(new Vector2(0f, 14f));
        ImGui.TextUnformatted("Recent projects");
        ImGui.Separator();

        var paths = _recent.Paths;
        if (paths.Count == 0) ImGui.TextDisabled("Nothing here yet. Create a project to get started.");

        ImGui.BeginChild("##recent");
        foreach (string path in paths) DrawRecentRow(path);
        ImGui.EndChild();
    }

    private void DrawRecentRow(string path)
    {
        ImGui.PushID(path);
        var start = ImGui.GetCursorScreenPos();
        float width = ImGui.GetContentRegionAvail().X;
        bool open = ImGui.Selectable("##row", false, ImGuiSelectableFlags.AllowOverlap, new Vector2(width, RowHeight));

        var list = ImGui.GetWindowDrawList();
        list.AddText(start + new Vector2(14f, 8f), ImGui.GetColorU32(ImGuiCol.Text), ProjectName(path));
        list.AddText(start + new Vector2(14f, 8f + 22f), ImGui.GetColorU32(ImGuiCol.TextDisabled), path);

        ImGui.SetCursorScreenPos(new Vector2(start.X + width - 96f, start.Y + (RowHeight - 28f) / 2f));
        if (ImGui.Button("Remove")) _recent.Remove(path);
        ImGui.SetCursorScreenPos(start + new Vector2(0f, RowHeight + 2f));
        ImGui.PopID();

        if (open) OpenProject(path);
    }

    private void DrawNewProject()
    {
        ImGui.TextUnformatted("New project");
        ImGui.Dummy(new Vector2(0f, 4f));

        ImGui.TextDisabled("Name");
        ImGui.SetNextItemWidth(360f);
        ImGui.InputText("##name", ref _name, 64, ImGuiInputTextFlags.AutoSelectAll);

        ImGui.Dummy(new Vector2(0f, 4f));
        ImGui.TextDisabled("Location");
        ImGui.SetNextItemWidth(520f);
        ImGui.InputText("##location", ref _location, 260, ImGuiInputTextFlags.AutoSelectAll);
        ImGui.SameLine();
        if (ImGui.Button("Browse...")) BrowseForLocation();

        bool valid = ProjectTemplate.IsValidName(_name, out string problem) && _location.Trim().Length > 0;
        ImGui.Dummy(new Vector2(0f, 6f));
        if (valid) ImGui.TextDisabled("The project will be created in " + Path.Combine(_location.Trim(), _name));
        else Warning(problem.Length > 0 ? problem : "Choose where to put the project.");
        if (_message.Length > 0) Warning(_message);

        ImGui.Dummy(new Vector2(0f, 10f));
        ImGui.BeginDisabled(!valid);
        if (AccentButton("Create")) CreateProject();
        ImGui.EndDisabled();
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            _page = Page.Home;
            _message = string.Empty;
        }
    }

    private void DrawBuild()
    {
        ImGui.TextUnformatted($"{(BuildFailed ? "The build failed" : "Building")}: {_buildTitle}");
        if (!BuildFailed) ImGui.TextDisabled("The editor opens when the build is done.");

        ImGui.Dummy(new Vector2(0f, 8f));
        ImGui.BeginChild("##buildlog", new Vector2(0f, ImGui.GetContentRegionAvail().Y - 48f), ImGuiChildFlags.Border);
        foreach (string line in _buildLog) ImGui.TextUnformatted(line);
        if (!BuildFailed) ImGui.SetScrollHereY(1f);
        ImGui.EndChild();

        if (BuildFailed && ImGui.Button("Back")) ClearBuild();
    }

    private static Vector4 Shade(Vector4 colour, float factor) => new(Math.Min(colour.X * factor, 1f), Math.Min(colour.Y * factor, 1f), Math.Min(colour.Z * factor, 1f), 1f);

    private static void Warning(string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.55f, 0.5f, 1f));
        ImGui.TextWrapped(text);
        ImGui.PopStyleColor();
    }

    private static bool AccentButton(string label)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, Win11Theme.Accent);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Shade(Win11Theme.Accent, 1.18f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Shade(Win11Theme.Accent, 0.82f));
        bool pressed = ImGui.Button(label);
        ImGui.PopStyleColor(3);
        return pressed;
    }
}
