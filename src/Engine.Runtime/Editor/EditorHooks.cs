using Engine.Rendering;

namespace Engine.Runtime.Editor;

/// <summary>What the editor needs from the game it edits. <c>Game</c> fills these in from its virtual members.</summary>
/// <param name="Camera">The camera that draws the scene, or null when the game has none (then the editor has no scene view, only the panels).</param>
/// <param name="ScenePath">The scene file Save and Reload use, or null when the game names none.</param>
/// <param name="Save">Writes the world to a scene file.</param>
/// <param name="Load">Replaces the world with a scene file and lets the game re-establish what it cached from the old world.</param>
internal sealed record EditorHooks(Func<Camera2D?> Camera, Func<string?> ScenePath, Action<string> Save, Action<string> Load);
