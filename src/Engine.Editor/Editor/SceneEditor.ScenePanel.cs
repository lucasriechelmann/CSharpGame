using System.Numerics;
using ImGuiNET;

namespace Engine.Editor;

// The scene panel: the game is drawn into a texture and shown here, so the editor's windows dock around the scene instead of covering it.
internal sealed partial class SceneEditor
{
    /// <summary>The title of the scene panel window; layouts refer to it by name.</summary>
    public const string ScenePanelTitle = "Scene";

    /// <summary>The smallest size the game is rendered at, so a collapsed or tiny panel never asks post-processing for an empty image.</summary>
    private const int MinimumGameSize = 64;

    /// <summary>A draw list for the scene view: it takes view coordinates and draws inside the scene panel, clipped to it. Valid only while the panel is being drawn.</summary>
    private ViewDraw SceneDraw() => new(ImGui.GetWindowDrawList(), _viewport.Origin);

    /// <summary>
    /// Draws the Scene panel: a toolbar (Play, Pause, Stop), then the game's image filling the rest. The game is told the panel's size, so
    /// its camera and post-processing follow the panel when it is resized or docked elsewhere. While editing, the mouse picks, pans and drags the
    /// gizmo inside the panel; while playing, the mouse belongs to the game.
    /// </summary>
    private void DrawScenePanel()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        bool open = ImGui.Begin(ScenePanelTitle, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        ImGui.PopStyleVar();
        if (!open)
        {
            ImGui.End();
            return;
        }

        DrawSceneToolbar();

        var origin = ImGui.GetCursorScreenPos();
        var size = ImGui.GetContentRegionAvail();

        int width = Math.Max((int)size.X, 1);
        int height = Math.Max((int)size.Y, 1);
        _game.SetViewport(origin, Math.Max(width, MinimumGameSize), Math.Max(height, MinimumGameSize));

        var texture = _game.PostProcess.OutputTexture; // read after SetViewport: a resize replaces the texture
        // The texture is stored bottom row first, so the image is drawn with its vertical texture coordinates reversed.
        if (texture != null) ImGui.Image((IntPtr)texture.Handle, new Vector2(width, height), new Vector2(0f, 1f), new Vector2(1f, 0f));
        else ImGui.Dummy(new Vector2(width, height));

        bool hovered = ImGui.IsItemHovered();
        _viewport.Update(origin, new Vector2(width, height), hovered);
        if (Playing)
        {
            if (hovered) ImGui.SetNextFrameWantCaptureMouse(false); // clicks over the scene go to the game, not to the panel
        }
        else
        {
            HandleViewport();
            _inspector.CommitPendingEdit();
            DrawSceneOverlay();
        }

        ImGui.End();
    }

    private void DrawSceneToolbar()
    {
        ImGui.SetCursorPos(new Vector2(6f, ImGui.GetCursorPosY() + 4f));
        if (ImGui.Button(Playing ? "Stop" : "Play")) TogglePlay();

        ImGui.SameLine();
        ImGui.BeginDisabled(!Playing);
        bool paused = _game.IsPaused;
        if (ImGui.Checkbox("Pause", ref paused)) _game.IsPaused = paused;
        ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.TextDisabled(Playing ? "Playing: changes are discarded on Stop." : "Editing: Middle-drag pans, wheel zooms, left click selects.");
        ImGui.SetCursorPos(new Vector2(0f, ImGui.GetCursorPosY() + 2f));
    }
}
