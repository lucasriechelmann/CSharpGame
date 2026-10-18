namespace Engine.Runtime;

/// <summary>
/// What a <see cref="Game"/> needs from an editor attached to it, and nothing more. The editor lives in its own program and plugs in
/// through this interface, so a shipped game contains no editor code and the loop does not know what the editor looks like.
/// </summary>
public interface IEditorShell
{
    /// <summary>True while the editor UI is shown (and <see cref="Draw"/> is called each frame).</summary>
    bool Active { get; }

    /// <summary>True while the game is frozen for editing, false while it plays inside the editor. Surfaces as <see cref="Game.IsEditing"/>.</summary>
    bool Editing { get; }

    /// <summary>Called first in every GUI pass, before the game's own windows are drawn, so the editor can arrange the window layout before those windows first appear.</summary>
    void BeginFrame();

    /// <summary>The F2 key: shows or hides the editor. A shell that is always on may ignore it.</summary>
    void Toggle();

    /// <summary>Draws the editor for one frame, between the ImGui frame begin and render.</summary>
    void Draw();
}
