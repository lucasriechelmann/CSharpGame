using Engine.Core.Ecs;
using Engine.Rendering;
using Engine.Rendering3D;

namespace Engine.Runtime;

/// <summary>What the editor needs from the game it edits. <c>Game</c> fills these in from its virtual members.</summary>
/// <param name="Camera">The 2D camera that draws the scene, or null when the game has none.</param>
/// <param name="Camera3D">The 3D camera that draws the scene, or null. When set, the editor gives the scene an orbit-camera 3D view instead of the 2D pan and zoom; with neither camera the editor has only its panels.</param>
/// <param name="ScenePath">The scene file Save and Reload use, or null when the game names none.</param>
/// <param name="Save">Writes the world to a scene file.</param>
/// <param name="Load">Replaces the world with a scene file and lets the game re-establish what it cached from the old world.</param>
/// <param name="AssetPath">The file to write an asset (a tile layer) to, given its asset name such as <c>levels/ground.layer.json</c>.</param>
/// <param name="AssetWritten">Tells the game the editor wrote an asset file, so its file watcher does not reload what the editor just saved.</param>
/// <param name="Snapshot">The whole world as scene JSON text, taken when Play starts.</param>
/// <param name="Restore">Replaces the world with scene JSON text (a <see cref="Snapshot"/>) and lets the game re-establish what it cached.</param>
/// <param name="PlayStarted">Tells the game the editor started playing, so it can begin a fresh run from the edited world.</param>
/// <param name="PlayStopped">Tells the game the editor stopped playing and the world was restored, so it can discard the run's state.</param>
/// <param name="PrefabOf">The prefab file an entity was made from, or null when it was not made from one.</param>
/// <param name="SaveAsPrefab">Writes an entity as a prefab file (a name relative to the asset root) and links the entity to it.</param>
/// <param name="RevertToPrefab">Puts a prefab instance back to what its prefab says.</param>
internal sealed record EditorHooks(
    Func<Camera2D?> Camera,
    Func<Camera3D?> Camera3D,
    Func<string?> ScenePath,
    Action<string> Save,
    Action<string> Load,
    Func<string, string> AssetPath,
    Action<string> AssetWritten,
    Func<string> Snapshot,
    Action<string> Restore,
    Action PlayStarted,
    Action PlayStopped,
    Func<Entity, string?> PrefabOf,
    Action<Entity, string> SaveAsPrefab,
    Action<Entity> RevertToPrefab);
