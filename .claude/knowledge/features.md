# Feature map

What each functional area does and which folders own it. Individual files are named only for entry points.

## Built

| Area | What it does | Owner | Entry points |
|---|---|---|---|
| Game loop | Window, fixed-timestep simulation with interpolation factor, per-frame update, render. Hooks: `Load`, `Update`, `FixedUpdate`, `Draw`, `DebugGui`, `Unload`. | `src/Engine.Runtime`, `src/Engine.Platform`, `src/Engine.Core/FixedTimestep.cs` | `Game`, `GameWindow` |
| Debug UI | Dear ImGui overlay (FPS, fixed steps, alpha, entities, assets, VSync); games add panels via `DebugGui`. F1 toggles. | `src/Engine.Runtime` | `DebugOverlay` |
| ECS | Entities, components, queries, systems, deferred changes. | `src/Engine.Core/Ecs` | `World`, `ISystem`, `SystemGroup`, `CommandBuffer` |
| Input | Keyboard/mouse state with per-frame edges, named actions and axes, ImGui capture respected, focus loss releases all. | `src/Engine.Core/Input`, `src/Engine.Platform` (adapter) | `InputState`, `ActionMap`, `SilkInputAdapter` |
| Assets | Cached, typed, handle-based loading and disposal. | `src/Engine.Core/Assets`, `src/Engine.Rendering` (texture loader) | `AssetManager`, `AssetHandle<T>`, `TextureLoader` |
| Sprite rendering | Batched sprites with tint, pivot, rotation, flip and sprite-sheet regions; 2D camera with zoom/rotation and screen-to-world conversion. | `src/Engine.Rendering` | `SpriteBatch`, `SpriteRenderSystem`, `Camera2D`, `Sprite` |
| Physics and collision | Gravity, terminal velocity, swept AABB-vs-tile and AABB-vs-static-box collision, persistent contact flags. | `src/Engine.Physics2D` | `PhysicsSystem`, `CollisionSolver`, `TileGrid` |
| Platformer controller | Acceleration, jump with coyote time, jump buffering, variable height, heavier fall; tuning editable live. | `src/Engine.Physics2D` | `PlatformerControllerSystem`, `PlatformerTuning` |
| Tilemaps | Sparse chunked tile layers, atlas tile sets with solid flags, camera-culled rendering in background/foreground passes, collision grid derived from a layer. | `src/Engine.Tilemaps` | `TileLayer`, `TileSet`, `TileLayerRenderSystem`, `TileGridBuilder` |
| Sprite animation | Frame clips (loop, once, ping-pong) and a parameter-driven state machine that picks the clip; per-entity playback with speed and finished flag. | `src/Engine.Animation` | `AnimationStateMachine`, `SpriteAnimator`, `AnimationSystem` |
| Scenes and prefabs | Save/load the whole world as JSON, entity templates (prefabs) with merge overrides and inheritance, assets referenced by name, all-or-nothing loading, JSON loaders for tile layers, tile sets and animation graphs. | `src/Engine.Scenes`, `src/Engine.Runtime` (`EngineComponents`, loader registration), loaders in `Tilemaps`/`Animation` | `SceneSerializer`, `ComponentRegistry`, `JsonMerge`, `Game.Scenes` |
| Post-processing | Scene drawn into an off-screen target when any effect is enabled; enabled effects ping-pong between two targets, the last writes to the window. Built-in: colour grade (brightness, contrast, saturation, tint), vignette, bloom (bright pass, half-resolution separable blur, add). All settings editable live in the "Post effects" window. | `src/Engine.Rendering/PostProcessing`, `src/Engine.Runtime` (frame hooks, panel) | `PostProcessChain`, `IPostEffect`, `SinglePassEffect`, `RenderTarget`, `Game.PostProcess` |
| 2D lighting | `Light2D` point lights (colour, intensity, radius, smooth falloff) drawn additively into a screen-sized light map cleared to the ambient colour, then multiplied over the scene; off-screen lights culled. No shadows. | `src/Engine.Rendering/Lighting` | `Light2D`, `LightingEffect`, `LightMath` |
| Entity inspector | Window listing every entity (named by its `Name` component) with filter, create and destroy; the selected entity's components are editable live through reflection (floats, ints, bools, strings, enums, vectors, quaternions as Euler degrees, colours, nested structs), components can be added from the registry and removed. | `src/Engine.Runtime/Inspector`, `World.Reflection.cs` in Core, `ComponentRegistry.Describe` | `EntityInspector`, `FieldEditor` |
| Profiler | Per-section timing (every system in the three groups plus `Game.Update/FixedUpdate/Draw`, `Debug UI` and `CPU frame`), 120-frame history, last/avg/max table and a frame graph. Games add sections. | `src/Engine.Core/Diagnostics`, `src/Engine.Runtime/Inspector` | `Profiler`, `SystemGroup.Profile`, `ProfilerPanel`, `Game.Profiler` |
| Audio | WAV (8/16-bit, mono/stereo) decoding into `AudioClip` assets, OpenAL playback with a 32-voice pool (oldest voice stolen when full), per-sound volume, pitch and loop, master volume, preload, silent fallback device when none exists. 2D only: no positional audio. | `src/Engine.Audio`, `src/Engine.Runtime` (loader, device lifetime, overlay line) | `IAudioDevice`, `OpenAlAudioDevice`, `AudioClip`, `Game.Audio` |
| Playable game (sample v1) | Collect every coin (25, placed as prefabs on the platforms) to open the exit, reach it to win; creatures hurt (3 lives, 1.5 s invulnerability, knockback, 2 s start grace) or die when stomped from above; score = coins + stomps + time bonus; HUD, win/lose screens, Enter restarts; sounds for every event; coins and lamps are `Light2D` lights. Quick-load re-syncs the coin count. | `samples/Platformer` | `GameSession`, `InteractionSystem`, `Coin`, `Goal`, `PlatformerGame.Flow.cs`, `PlatformerGame.Hud.cs` |
| Sample game | Data-driven tilemap level (`Assets/scenes/level.scene.json` plus prefabs, layers, tile set, animations), animated crowned hero you run and jump with, animated patrolling creatures spawned from a prefab, F5/F9 quick-save and quick-load of the whole world, scroll or Q/E to zoom, camera follows hero, live "Hero feel" panel. Pixel art is generated by `tools/make-sprite-sheets.ps1`; the JSON content is hand-editable. | `samples/Platformer` | `PlatformerGame` (partials), `Assets/scenes/level.scene.json`, `Program.cs` |
| Visual verification | Scripts to launch, drive and screenshot the sample, and to regenerate its art and sounds. | `tools/` | `tools/README.md` |

## Roadmap

Status of the phased plan (update when a phase lands or the plan changes):

1. Foundation: window, fixed-timestep loop, ImGui debug UI. **Done**
2. Sparse-set ECS with tests. **Done**
3. GL wrapper, shaders, textures, batched sprite renderer, 2D camera. **Done**
4. Input mapping, asset cache, sprite demo. **Done**
5. AABB/tile collision, own simple physics, platformer controller (coyote time, jump buffering). **Done** (spatial hash, one-way platforms, slopes, moving platforms deferred)
6. Tilemaps (chunked, texture atlas) and sprite animation (clips, state machines). **Done** (tile flip flags, animated tiles, parallax, cached chunk meshes and Tiled import deferred)
7. JSON scenes and prefabs (System.Text.Json, components as serializable data, assets saved by name). **Done** (entity references, scene-in-scene, instance-to-prefab links and binary scenes deferred)
8. ImGui entity inspector/profiler, OpenAL audio. **Done** (click-to-select entities in the world, undo/redo, prefab-aware editing, GPU timing, audio streaming for music, positional audio, an audio component and mixer groups deferred)
9. Framebuffers, post-processing, 2D lights. **Done** (light shadows/occluders, normal-mapped lighting, HDR targets, render-to-texture for game code, MSAA, per-camera effects and a pixel-perfect low-resolution mode deferred)
10. Playable platformer v1. **Done** (single level, no save of high scores, no enemy variety, no moving platforms or checkpoints, no menus or pause; kept deliberately small); then a full editor and 3D.

Known gaps to keep in mind: lights do not cast shadows (light passes through walls) and the light map is 8-bit so overlapping lights clamp at white, post effects work on the whole window (no per-camera or per-layer effects) and run at window resolution, audio decodes whole files into memory (no streaming, so long music is costly), no positional audio, sounds are fire-and-forget (no audio component or per-entity voices), the inspector cannot select by clicking in the world and its edits are not undoable, no render interpolation between fixed steps yet (needs a previous-transform component), no hot-reload of assets, no logging, high-DPI untested, tiles are submitted as one quad per visible tile (no cached chunk meshes), bodies spawned inside solids are not pushed out, animation has no events/blending and one transition per update.
