using Engine.Core.Assets;
using Engine.Core.Ecs;
using Engine.Core.Logging;
using Engine.Runtime.Inspector;

namespace Engine.Runtime;

/// <summary>Everything an editor needs from the game it is attached to: the world and assets to edit, the shared inspector, the log, and the hooks into the game's virtual members.</summary>
/// <param name="World">The game's entity world.</param>
/// <param name="Assets">The game's asset manager.</param>
/// <param name="Inspector">The entity inspector the game's debug overlay also uses.</param>
/// <param name="Log">The game's logger.</param>
/// <param name="Hooks">Camera, scene file, save/load and play callbacks.</param>
/// <param name="Game">The game itself, for the editor's own panels (window, post-processing, profiler).</param>
internal sealed record EditorContext(World World, AssetManager Assets, EntityInspector Inspector, Logger Log, EditorHooks Hooks, Game Game);
