# Architecture

Learning-oriented 2D game engine in C# (.NET 10, Windows), designed so 3D can be added later. Goal: understand how engines work, so core systems are written from scratch and libraries are used only for plumbing (windowing, GL bindings, image decoding, debug UI).

## Projects and dependencies

```
samples/Platformer ──► Engine.Runtime ──► Engine.Rendering ──► Engine.Core
        │                   │                                     ▲
        │                   └──────────► Engine.Platform ─────────┤
        ├──► Engine.Physics2D ────────────────────────────────────┤
        ├──► Engine.Scenes ──► Engine.Core   (Runtime also depends on Scenes, Physics2D, Tilemaps, Animation, Audio)
        ├──► Engine.Audio ──► Engine.Core
        ├──► Engine.Tilemaps ──► Engine.Rendering, Engine.Physics2D, Engine.Core
        ├──► Engine.Animation ──► Engine.Rendering, Engine.Core
        └──► Engine.Rendering, Engine.Core (direct, for components/types)
tests/<Module>.Tests ──► <Module> (+ the modules it needs): one test project per module
```

| Project | Role | Key packages |
|---|---|---|
| `src/Engine.Core` | GPU- and window-free logic: ECS, fixed timestep, `Transform`, input state and action map, asset manager. Fully unit-testable. | none |
| `src/Engine.Platform` | Silk.NET window, GL context, input adapter, ImGui controller. The only place that touches Silk windowing/input. | Silk.NET.Windowing/Input/OpenGL, Silk.NET.OpenGL.Extensions.ImGui |
| `src/Engine.Rendering` | GL wrappers and 2D rendering: `Shader`, `Texture2D`, `SpriteBatch`, `Camera2D`, `Sprite` component, `SpriteRenderSystem`, `TextureLoader`. Unsafe blocks allowed (GL pointer APIs only). | Silk.NET.OpenGL, StbImageSharp |
| `src/Engine.Physics2D` | Own 2D physics, no GPU: `Aabb`, `TileGrid`, `CollisionSolver` (swept per-axis), `PhysicsSystem`, `PlatformerController`/`PlatformerControllerSystem`. Decision: physics is a separate module (not in Core) so Core stays tiny and a future 3D physics can sit beside it; it never references Rendering. | none |
| `src/Engine.Tilemaps` | Sparse chunked tile layers, tile sets (atlas layout + per-tile solidity), layer-to-`TileGrid` builder, tile render system. Decision: separate from Rendering (which stays GL-focused) and from Physics2D (which must not know about tilemaps; the builder lives here and depends on Physics2D). | none |
| `src/Engine.Animation` | Clips, state machine, animator component and system. Drives `Sprite.Source`, so it depends on Rendering but not on GL. | none |
| `src/Engine.Scenes` | Save/load a `World` as JSON, prefabs with merge overrides, component registry. Knows no component types; only depends on Core (System.Text.Json is in the BCL). | none |
| `src/Engine.Audio` | `AudioClip` (decoded 16-bit PCM, no OpenAL objects), `WavDecoder`, `AudioClipLoader`, `IAudioDevice` with `OpenAlAudioDevice` (sources/buffers via Silk.NET.OpenAL, unsafe only for the buffer upload) and `NullAudioDevice`, `VoicePool` (pure voice bookkeeping). Decision: audio is its own module because it has no window or GL dependency and another backend would sit behind `IAudioDevice`; clips are pure data so they can be decoded and tested without a sound card. | Silk.NET.OpenAL, Silk.NET.OpenAL.Soft.Native |
| `src/Engine.Runtime` | `Game` base class (the loop), `DebugOverlay`, `FrameStats`, `EngineComponents`, `Inspector/` (`EntityInspector`, `FieldEditor`, `ProfilerPanel`). **Composition root: the one module allowed to depend on every engine module** (decision: it registers every engine component and every asset loader (texture, tile set, tile layer, animation, prefab) so games need no boilerplate; the audio clip loader and the audio device follow the same rule). | ImGui.NET via Silk ImGui extension |
| `samples/Platformer` | The first game; consumes the engine like any game project. | none beyond engine |

Rules: Core never references Platform, Rendering or Silk.NET. Layers depend on abstractions at their boundaries (input is `InputState`/`ActionMap`, not Silk types).

Shared build config is in `Directory.Build.props` (net10.0, nullable, implicit usings; for non-test projects XML docs + StyleCop `SA1402`, with `CS1591` and `SA1402` as errors). Analyzer severities are in `.editorconfig`.

## Frame data flow

Per display frame (`Game.OnUpdate` then `Game.OnRender`, driven by Silk's window loop):

1. Window events fill `InputState` (via `SilkInputAdapter`; key/mouse presses are dropped while ImGui wants the input, releases always pass).
2. `FrameSystems` run, then `Game.Update` (read input here, latch intent into components, e.g. `PlayerInputSystem` writes `PlatformerIntent`).
3. `FixedTimestep.Advance(dt)` returns N steps; for each: `FixedSystems`, then `Game.FixedUpdate`. The sample's first fixed system is `PreviousTransformSystem` (records each `PreviousTransform` before the step moves anything).
4. `Input.EndFrame()` clears pressed/released edges and scroll.
5. Render: `Game` sets `RenderClock.Alpha` (the timestep's 0..1 fraction), then `PostProcess.BeginScene()` (redirects drawing into the scene target only if some effect is enabled), `Game.Draw` (resets batch stats, clears), `RenderSystems` in registration order (sample: background/ground `TileLayerRenderSystem`, `SpriteRenderSystem` which gathers `Transform`+`Sprite` and sorts back-to-front by Z then texture, foreground `TileLayerRenderSystem`; each pass is its own `SpriteBatch.Begin/End`), then `PostProcess.EndScene()` (runs the enabled effects, the last one writing to the window), then the ImGui overlay (`DebugOverlay` + `Game.DebugGui`), which is drawn after the effects so tools are never darkened or bloomed.

Shutdown: GPU cleanup happens in the window's `Closing` event, **while the GL context still exists**: `GameWindow.OnClosing` raises `Closing` (so `Game` runs `Unload()` then `Assets.Dispose()`), then disposes the ImGui controller and input. `Game.Dispose`/`GameWindow.Dispose` afterwards only release the managed GL bindings and the window. (Decision and reason: after `Run()` returns the context is destroyed and any GL delete throws `NoContext`; this crashed every normal window close until phase 7.)

## Render interpolation

Fixed steps and display frames do not line up, so drawing the raw simulated position makes motion judder. Entities with a `PreviousTransform` are drawn at `Lerp(previous, current, alpha)` (`PreviousTransform.Interpolate`; scale is not blended). Decisions and reasons: (1) it is an opt-in component rather than a second copy of every `Transform`, so static entities pay nothing; (2) `default` means "no history", so a spawn, respawn or scene load draws at the real position instead of sweeping in from the origin; (3) `RenderClock` (set by `Game` before `Draw`) is handed to the render systems by constructor, not read from a global, and the parameter is optional so systems without it draw the raw state; (4) the camera follow runs in `Game.Draw` (after the steps, with the current alpha) on the interpolated hero position, otherwise it chases a position the sprite is not drawn at and jitters; (5) `LightingEffect` interpolates lights the same way so the lantern stays on the hero.

## ECS (`src/Engine.Core/Ecs`)

- `Entity` = (Id, Generation); generation 0 is `Entity.Null`. `EntityAllocator` hands out ids and bumps generations on destroy so stale handles fail `IsAlive`.
- `World` (partial: `World.cs` lifetime, `.Components.cs`, `.Queries.cs`, `.Internals.cs`) owns one `ComponentPool<T>` per component type, stored in a `Dictionary<Type, IComponentPool>`.
- `ComponentPool<T>` is a sparse set: `_sparse[entityId] = denseIndex + 1`, packed `Dense`/`Components` arrays, swap-remove.
- Queries (`Query<T1>`, `<T1,T2>`, `<T1,T2,T3>`) pass components by `ref`; they take an iteration lock so `Create/Destroy/Add/Remove` throw mid-query. Use `CommandBuffer` to defer, `Set` to overwrite existing components safely.
- Systems implement `ISystem.Update(World, double dt)`; `SystemGroup` runs them in insertion order. `Game` exposes three groups: `FixedSystems`, `FrameSystems`, `RenderSystems`.

## Physics (`src/Engine.Physics2D`)

- **Per fixed step** (`PhysicsSystem`): for each `Transform`+`PhysicsBody`+`BoxCollider`: apply gravity (`PhysicsSettings.Gravity`, `GravityScale`, clamp to `MaxFallSpeed`), sweep X then Y with `CollisionSolver.Sweep` (zero the blocked velocity component, snap to the face), then probe a 0.01 skin in four directions to set `PhysicsBody.Contacts` (so contacts persist while resting against a surface, even with zero velocity or zero gravity).
- **Solids**: `TileGrid` (cell (0,0) bottom-left, outside = empty) plus `StaticSolid`-tagged `BoxCollider` entities gathered each step (brute-force list; a spatial hash is deferred until static box counts justify it).
- **Sweep algorithm**: per axis, find the nearest obstacle face ahead of the leading edge across the box's extent on the other axis (shrunk by `Epsilon` = 0.001 so touching is not overlapping); obstacles already behind the leading edge are ignored, so overlapping a solid lets a body leave it but also lets a body spawned inside pass through. Tunnelling-proof for any speed.
- **Platformer controller**: input layer writes `PlatformerIntent` per frame (`MoveX`, `JumpHeld`, sticky `JumpPressedPending`); fixed-step `PlatformerControllerSystem` (before `PhysicsSystem`) converts the pending flag to a jump buffer, tracks coyote time from last step's `Below` contact, accelerates toward target speed (ground/air), jumps when buffer and coyote are both positive, cuts the jump when released early, and raises gravity scale while falling. Tuning (`PlatformerTuning`) and timers live in one `PlatformerController` component (the ECS supports queries of up to 3 components).
- **Not yet**: one-way platforms, slopes, moving platforms/carrying, body-vs-body collision, depenetration.

## Tilemaps (`src/Engine.Tilemaps`)

- `TileLayer` stores tile ids in a `Dictionary<long, TileChunk>` of 16x16 chunks keyed by packed chunk coordinates; chunks appear when a tile is placed and vanish when empty. Cell (0,0) is bottom-left, Y up, negatives allowed (`cell >> 4` and `& 15` floor correctly). The renderer reads chunks through `TryGetChunkTiles` (a `ReadOnlySpan<int>`, no copies).
- `TileSet` = atlas texture handle + tile size/columns/rows/spacing/margin; ids are 1-based, row-major; per-tile `IsSolid`.
- Layers and tile sets are **assets** (`Assets.Add("layer/ground", layer)`), referenced from the `TileLayerRenderer` component by handle. The layer entity's `Transform` position is the world position of cell (0,0); its Z is draw depth. `TileLayer.TileSize` is world units per tile (art is 16 px drawn at 32).
- `TileLayerRenderSystem` collects layers for its pass (`foreground` flag), sorts by Z, asks `Camera2D.GetVisibleBounds` for the visible world box, converts to a chunk range, and submits one `SpriteDraw` per visible tile to the shared `SpriteBatch` (UV rect inset by 0.02 px to stop atlas bleeding). Cached per-chunk GPU meshes are a later optimisation.
- `TileGridBuilder.FromLayer(layer, tileSet, layerOrigin)` makes the physics `TileGrid` from the layer's used bounds and the tile set's solid flags.

## Animation (`src/Engine.Animation`)

- `AnimationClip` = pixel rects + fps + `AnimationLoopMode` (Loop/Once/PingPong); `GetFrameIndex(time, out finished)` is pure.
- `AnimationStateMachine` (shared asset): float parameters (booleans are 0/1), states (each a clip), and ordered transitions (`From` a state or any, `To`, conditions on parameters, optional `RequiresFinished`). First matching transition wins; any-state transitions never target the current state. One transition per update.
- `SpriteAnimator` component = machine handle, current state, state time, speed, finished flag, parameter array (create with `SpriteAnimator.Create`). `AnimationSystem` (frame group) advances time, evaluates transitions, writes `Sprite.Source`. Game systems write parameters first (sample: `HeroAnimationSystem` from `PhysicsBody`).

## Scenes and prefabs (`src/Engine.Scenes`)

- **File shapes** (camelCase, indented). Scene: `{ "version": 1, "entities": [ { "prefab": "optional.prefab.json", "components": { "Transform": { "position": { "x": 1 } } } } ] }`. A prefab file is the same entity shape (and may itself name a `prefab`, depth limit 16). Asset data files: `.layer.json` (rows of tile ids, top row first, plus `originX/originY` of the bottom-left cell), `.tileset.json` (texture name, tile size, solid ids), `.anim.json` (parameters, states with `grid` or `frames` clips, transitions with conditions by name).
- **Components**: `ComponentRegistry.Register<T>(name)` builds a `TypedComponentSerializer<T>` (System.Text.Json with `IncludeFields`, camelCase, enums as text, computed read-only properties skipped). Only registered components are saved; unknown names in files throw naming the component. Names are stable file names, independent of the C# type. `Runtime.EngineComponents.RegisterAll` registers the engine's ten; games add their own (sample: `Patrol`).
- **Assets by name**: `AssetHandleConverter<T>` writes `assets.NameOf(handle)` (or `null`) and reads via `assets.Load<T>(name)`, which returns cached/in-memory assets first and otherwise loads the file. So anything a scene refers to needs a registered `IAssetLoader<T>`.
- **Load is two-phase**: resolve prefab chains and parse every component into a ready-to-apply action first (errors surface here, world untouched), then create entities. Prefab override = `JsonMerge.Apply` (RFC 7386: objects merge recursively, arrays/scalars replace, `null` removes). Prefabs are cached assets; resolution works on `DeepClone`d copies so cached templates are never mutated.
- **Save** writes every entity in id order with its registered components (`SerializeEntity` returns a valid prefab body). Quick-save in the sample writes `saves/quicksave.scene.json` next to the exe; quick-load does `World.Clear()` then `LoadFile`.
- **Limits**: no `Entity` references between components (not remapped), no scene-in-scene, no instance-to-prefab link kept after loading (instances are expanded), no editor yet.

## Diagnostics and tools (`Engine.Core/Diagnostics`, `Engine.Core/Logging`, `Engine.Runtime/Inspector`)

- **Logging**: `Logger` (Core, no Silk.NET) filters by `MinimumLevel` (default Info) and fans each `LogEntry` (time, level, category, message) out to its `ILogSink`s; sink exceptions propagate (fail loudly). `Game.Log` is created with a `ConsoleLogSink` and a 500-entry `MemoryLogSink` (ring buffer, so memory stays constant) that feeds `LogPanel`. Decision: an instance owned by `Game` rather than a static `Log` class, so tests and tools can build their own loggers and sinks, and a file or editor sink is one `AddSink` away. The clock is injectable for exact timestamps in tests.

- **Profiler**: sections are registered once by name and timed by integer id (`Begin()` returns a tick count, `End(section, start)` adds to that frame's total). `BeginFrame()` rolls the totals into a 120-frame ring buffer per section. Sections that run several times per frame (fixed systems on a multi-step frame) sum. The clock is injected (`Func<long>` plus ticks per second) so tests use exact values. `SystemGroup.Profile(profiler, "Fixed")` resolves a `group/SystemType` section per system up front, so a profiled run allocates nothing. `Game` calls `BeginFrame()` at the start of `OnUpdate` and closes the `CPU frame` section at the end of `OnGui` (the last callback of a frame, so it also works with the overlay hidden; it excludes the vsync wait in the buffer swap).
- **Type-erased component access** (`World.Reflection.cs`): `GetComponentTypes`, `GetBoxed`, `SetBoxed`, `AddBoxed`, `Remove(entity, Type)` (pools gained `ComponentType`/`GetBoxed`/`SetBoxed`/`AddDefault`). They box and allocate, so they are for tools only. Structural ones respect the query lock; `SetBoxed`, like `Set`, is allowed mid-query.
- **Inspector**: `ComponentRegistry.Describe()` supplies name, type and a valid default value (`Register<T>(name, defaultValue)`, e.g. `Transform.Identity`) for the Add menu. `FieldEditor` draws one widget per type (boxed `object` in, changed flag out) and recurses into structs: public-field structs are edited in place on the box, positional record structs (`Rect`, `Color`) are edited per constructor parameter and rebuilt through the constructor. Quaternions go through `QuaternionMath` (Euler degrees, pitch clamped at +-90). The inspector copies the component out, edits, and writes back only when something changed.
- **Audio flow**: `Game.OnLoad` registers `AudioClipLoader` and creates the device with `OpenAlAudioDevice.TryCreate` (a missing OpenAL library or output device yields a `NullAudioDevice` whose `Description` explains why; the overlay shows it). `OnUpdate` calls `Audio.Update()` (frees finished voices). `OnClosing` disposes the device after `Unload` and before `Assets`. `Play` takes a free voice or steals the oldest (`VoicePool`), buffers upload on first play or `Preload`, and handles are generation-checked so a stale `VoiceId` never stops someone else's sound.

## Sample game rules (`samples/Platformer`)

- **Split of responsibilities**: `GameSession` (pure: lives, score, coins, clock, `GamePhase`) owns every rule and has no engine types. `InteractionSystem` (fixed group, after `PhysicsSystem`) only detects hero overlaps (`BoxCollider.GetBounds` / `Aabb.Overlaps`) with `Coin`, `Patrol` (the creature marker) and `Goal` entities and reports events to the session; removals go through a `CommandBuffer` after the queries. A stomp = hero moving down with its feet above the creature's middle; anything else is a hit (knockback away from the creature). `PlatformerGame.Flow.cs` ticks the session each frame, zeroes the hero's `PlatformerIntent` once the run is over, swaps the exit door art when the last coin is taken, blinks the hero while invulnerable, and restarts by `World.Clear()` + `Scenes.LoadFile` (the same path as quick-load, which also re-syncs the coin count from the coins left in the world).
- **Content as data**: `coin.prefab.json` and `goal.prefab.json` plus scene entries that override only the position; `Coin` and `Goal` are registered components, so quick-save/load and the inspector handle them with no extra code. The locked/open door is two cells of `items.png`, chosen by setting `Sprite.Source`.
- **UI**: `Game.Hud()` (new hook, called every frame before the debug panels, so it survives F1) draws the status line and end message with ImGui windows flagged no-decoration/no-input; the font scale is set per window.

## Components (data)

| Component | Defined in | Notes |
|---|---|---|
| `Transform` | `src/Engine.Core/Components/Transform.cs` | `Vector3` position (Z = draw order), `Quaternion` rotation, `Vector3` scale. Never use `default`; use `Identity`/`At`. |
| `Sprite` | `src/Engine.Rendering/Sprite.cs` | `AssetHandle<Texture2D>`, size, pixel-space `Source` rect (empty = whole texture), tint, pivot, flip. |
| `BoxCollider`, `PhysicsBody`, `StaticSolid` (tag) | `src/Engine.Physics2D/` | Collider size/offset; velocity, gravity scale and contact flags; immovable solid marker. |
| `PlatformerController`, `PlatformerIntent` | `src/Engine.Physics2D/` | Tuning + runtime timers; player intent written by an input system. |
| `Name` | `src/Engine.Core/Components/` | Human-readable label (tools, inspector, saves). |
| `PreviousTransform` | `src/Engine.Core/Components/` | Position/rotation at the start of the last fixed step; `default` = no history yet (draws at the current transform, so teleports never smear). Opt-in for moving entities (hero, creatures). |
| `TileLayerRenderer` | `src/Engine.Tilemaps/` | Layer + tile set handles, tint, foreground flag. |
| `SpriteAnimator` | `src/Engine.Animation/` | Machine handle, state, time, speed, parameters. |
| `Patrol` | `samples/Platformer/` | Game-specific walking AI; also marks an entity as a creature for `InteractionSystem`. |
| `Coin`, `Goal` | `samples/Platformer/` | Collectible with a score value; tag for the level exit. |
| `Light2D` | `src/Engine.Rendering/Lighting/` | Point light: colour, intensity, radius. |

## Assets

`AssetManager` (Core) caches by normalised relative path per asset type, using registered `IAssetLoader<T>`s; `AssetHandle<T>` is a 1-based int id (0 = invalid) into a per-type `AssetStore<T>`. `Add(name, obj)` registers procedural assets. `Game.Assets` roots at `<exe dir>/Assets`; sample assets are copied there by `Platformer.csproj`. The manager owns and disposes `IDisposable` assets.

## Rendering pipeline

- **Post-processing** (`Engine.Rendering/PostProcessing`): `RenderTarget` = framebuffer object + `Texture2D` colour attachment (RGBA8, linear filter; `Resize` recreates it). `FullscreenQuad` draws one oversized triangle from `gl_VertexID` (no vertex buffer, only the empty VAO core profile requires), and its vertex source is shared by every effect, which supplies `vUv`. `PostProcessChain` owns two window-sized targets: `BeginScene` binds target 0 when any effect is enabled, `EndScene` runs the enabled effects ping-ponging between the targets, the last writing to framebuffer 0. A shader cannot read what it writes, hence two targets. Textures sampled from a target are not flipped: render and sample share GL's bottom-left origin. `Game` resizes the chain from the window's resize event, times it as the `Post process` profiler section and disposes it after `Unload`.
- **Effects**: `SinglePassEffect` (base: one shader, `SetUniforms` hook) for colour grade and vignette; `BloomEffect` implements `IPostEffect` itself (bright pass to a half-resolution target, N rounds of separable 9-tap Gaussian, composite). Settings are public properties; `Runtime/Inspector/PostEffectsPanel` edits them by reflection through `FieldEditor`.
- **Lighting**: `LightingEffect` (first in the chain) rebuilds a screen-sized light map each frame: clear to `Ambient`, then a `SpriteBatch` pass with `SpriteBlend.Additive` (blend `SrcAlpha, One`) draws one tinted radial-gradient quad per `Light2D` in view (gradient texture from `LightMath.BuildGradientPixels`, falloff `(1 - d^2)^2`), through the scene camera so lights align with sprites. The composite shader outputs `scene * lightMap * Exposure`. Decision: a light-map multiply rather than per-sprite lighting, because its cost does not grow with sprite count and the sprite renderer is untouched; the price is no shadows and no normal maps.

- World space: X right, Y up, Z draw order (larger = in front). `Camera2D` zoom 1 = 1 world unit per pixel; `GetViewProjection()` is row-vector `Matrix4x4`, uploaded to GL without transpose and used as `uViewProjection * vec4(pos, 1)`.
- `SpriteQuad.Write` (pure, tested) builds 4 vertices from a `SpriteDraw`; `SpriteBatch` fills a `SpriteVertex[]` (max 8192 sprites, 16-bit indices), uploads with `glBufferSubData`, draws with a static index buffer, flushing on texture change. Alpha blending on, depth test off. Built-in GLSL 330 shader in `SpriteShaderSource`.
- Texture space: (0,0) top-left, image row 0 is the top. Window framebuffer size is assumed equal to window size (inferred; not tested on high-DPI displays).

## External services

None (no network, database or config files).

## Planned (from the roadmap, not built yet)

Light occluders/shadows, an editor that builds on the inspector, music streaming and positional audio, and a 3D renderer beside `Engine.Rendering`.
