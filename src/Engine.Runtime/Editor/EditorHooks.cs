using Engine.Rendering;

namespace Engine.Runtime.Editor;

/// <summary>What the editor needs from the game it edits. <c>Game</c> fills these in from its virtual members.</summary>
/// <param name="Camera">The camera that draws the scene, or null when the game has none (then the editor has no scene view, only the panels).</param>
/// <param name="ScenePath">The scene file Save and Reload use, or null when the game names none.</param>
/// <param name="Save">Writes the world to a scene file.</param>
/// <param name="Load">Replaces the world with a scene file and lets the game re-establish what it cached from the old world.</param>
/// <param name="AssetPath">The file to write an asset (a tile layer) to, given its asset name such as <c>levels/ground.layer.json</c>.</param>
/// <param name="Snapshot">The whole world as scene JSON text, taken when Play starts.</param>
/// <param name="Restore">Replaces the world with scene JSON text (a <see cref="Snapshot"/>) and lets the game re-establish what it cached.</param>
/// <param name="PlayStarted">Tells the game the editor started playing, so it can begin a fresh run from the edited world.</param>
/// <param name="PlayStopped">Tells the game the editor stopped playing and the world was restored, so it can discard the run's state.</param>
internal sealed record EditorHooks(
    Func<Camera2D?> Camera,
    Func<string?> ScenePath,
    Action<string> Save,
    Action<string> Load,
    Func<string, string> AssetPath,
    Func<string> Snapshot,
    Action<string> Restore,
    Action PlayStarted,
    Action PlayStopped);
