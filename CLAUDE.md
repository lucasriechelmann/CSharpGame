# CSharpGame: a learning-oriented 2D game engine in C# (3D-ready)

Windows, .NET 10, Silk.NET (OpenGL 3.3 core), custom sparse-set ECS, Dear ImGui debug UI.
Goal: learn how engines work, so write core systems from scratch and use libraries only for plumbing. First game: a platformer. Status: phases 1-8 of 10 done (see `.claude/knowledge/features.md`).

## Projects

- `src/Engine.Core`      ECS, fixed timestep, `Transform`, input state and action map, asset manager. No GPU or window dependencies; unit-testable.
- `src/Engine.Platform`  Silk.NET window, GL context, input adapter, ImGui controller.
- `src/Engine.Rendering` Shader, Texture2D, SpriteBatch, Camera2D, `Sprite` component, `SpriteRenderSystem`, `TextureLoader`.
- `src/Engine.Physics2D` AABB, tile grid, swept collision solver, `PhysicsSystem`, platformer controller (coyote time, jump buffering, variable jump). No GPU or window dependencies.
- `src/Engine.Tilemaps`  Chunked `TileLayer`, `TileSet` atlas, `TileGridBuilder` (layer -> physics grid), `TileLayerRenderSystem`.
- `src/Engine.Animation` `AnimationClip`, data-driven `AnimationStateMachine`, `SpriteAnimator` component, `AnimationSystem`.
- `src/Engine.Scenes`    JSON scenes and prefabs: `ComponentRegistry`, `SceneSerializer`, `Prefab`, `JsonMerge`. Depends on Core only.
- `src/Engine.Audio`     `AudioClip`, `WavDecoder`, `IAudioDevice` (`OpenAlAudioDevice`, `NullAudioDevice`), `VoicePool`. Depends on Core and Silk.NET.OpenAL only; no window or GL.
- `src/Engine.Runtime`   `Game` base class and loop, debug overlay, entity inspector and profiler panel (`Inspector/`), `EngineComponents` (registers every engine component), `Game.Scenes`, `Game.Audio`, `Game.Profiler`. Composition root: the only module allowed to depend on every engine module; it also registers all asset loaders.
- `samples/Platformer`   The first game, almost entirely data: scene, prefabs, tile layers, tile set and animation graphs are JSON under `Assets/`.
- `tests/`               xUnit tests, one project per module (`Engine.<Module>.Tests`).
- `tools/`               PowerShell scripts to run, drive and screenshot the sample (see `tools/README.md`).

Dependency direction: samples -> Runtime -> (Core, Platform, Rendering, Scenes, Physics2D, Tilemaps, Animation, Audio); Tilemaps -> (Core, Rendering, Physics2D); Animation -> (Core, Rendering); Rendering, Platform, Physics2D, Scenes and Audio -> Core. Core never references any other module or Silk.NET; Physics2D never references Rendering or Tilemaps; Scenes knows no component types (Runtime registers them).

## Standing rules

- **Search for an existing implementation before creating a new one** (type, helper, system, test fixture). Extend or reuse; do not duplicate.
- **One type per file**, named after the type. *(Build error: SA1402.)*
- **XML-document everything public/protected**: purpose and why, not just the name. *(Build error: CS1591.)*
- **Split big classes into partial files** `Type.Topic.cs` (past ~150 lines or mixed topics); extract a collaborator when a responsibility is separable.
- **SOLID.** Depend on abstractions at layer boundaries (input, rendering, audio, physics); no Silk.NET types in game or Core code; small interfaces; extend by adding systems/components, not by editing switches.
- **Components are plain data structs**; behaviour lives in systems. Components reference assets by `AssetHandle<T>`, never by the loaded object.
- **3D-ready math**: `Vector3`/`Quaternion`/`Matrix4x4` for transforms (Z = draw order in 2D).
- **No allocation in per-frame/per-entity paths** (no LINQ, no per-frame capturing closures).
- **Structural ECS changes during a query** go through `CommandBuffer`.
- **Input**: ask `ActionMap` for intent, never a physical key. Read edges (pressed/released) in per-frame code and latch into components; never in fixed-step systems.
- **Physics**: order fixed systems as "decide velocities" (patrol, `PlatformerControllerSystem`) then `PhysicsSystem`. Gameplay reads `PhysicsBody.Contacts`/`IsGrounded`. Touching edges never count as overlap (epsilon); a body spawned *inside* a solid passes through it, so spawn clear of solids.
- **Tilemaps**: tile id 0 = empty, 1+ index a `TileSet` (row-major, 1-based); solidity lives on the tile set and collision is derived with `TileGridBuilder`, never hand-built twice. Draw order: background `TileLayerRenderSystem` -> `SpriteRenderSystem` -> foreground `TileLayerRenderSystem`.
- **Animation**: gameplay writes animator *parameters* (`SpriteAnimator.SetParameter`); the state machine picks the clip. Run the parameter-writing system before `AnimationSystem`, both per frame. Create animators with `SpriteAnimator.Create`.
- **Scenes and prefabs**: a component is saved only if registered (`Components.Register<T>("Name")`, in the game's constructor for game components); register it **and** keep it plain public-field data. Files reference assets by *name*, never id; prefab overrides are JSON merge (objects merge, arrays replace, `null` removes a component). Loading is all-or-nothing: parse first, create entities after. Do not store `Entity` references in saved components (not remapped on load yet). New asset kinds that scenes refer to need an `IAssetLoader<T>` registered in `Game.OnLoad`.
- **Profiling**: systems are timed automatically (`SystemGroup.Profile`); time your own work with `Profiler.GetSection` once (keep the id) then `Begin`/`End` per use. Never look sections up by name per frame.
- **Audio**: game code plays sounds through `Game.Audio` (`IAudioDevice`), never OpenAL types; load clips with `Assets.Load<AudioClip>("sounds/x.wav")` and `Audio.Preload` them in `Load`. A missing sound card falls back to `NullAudioDevice` (visible in the overlay), never a crash.
- **Inspector**: it edits registered components by reflection over their public fields, so keep component fields to simple types (numbers, bools, strings, enums, vectors, `Color`, `Rect`, `AssetHandle<T>`, nested plain structs); anything else shows read-only. Pass `defaultValue` to `Register<T>` when `default(T)` is invalid.
- **Shutdown**: free GPU resources in `Unload`/the window `Closing` event, while the GL context still exists (`Game` then disposes `Audio`, then `Assets`). Never touch GL from `Dispose` or after `Run()` returns.
- **Render stats**: call `SpriteBatch.ResetStats()` once per frame (in `Draw`) because a frame has several batch passes.
- **GPU resources** are `IDisposable`, owned by exactly one object; create in `Load`, dispose in `Unload`; `Game.Assets` disposes loaded assets.
- **Fail loudly** with specific guard-clause exceptions; never swallow exceptions.
- **Gotchas**: `Matrix4x4` is row-vector/row-major, upload to GL **without transposing**; `default(Transform)` is invalid, use `Transform.Identity`/`At`; use binary-exact floats in timing tests.
- Verify rendering and input changes in the real window with `tools/` (unit tests do not cover GL code).

## Read the knowledge files when

- Before adding or changing a data entity/component, a service or project boundary, a cross-project dependency, the frame loop, or the asset/ECS internals, read `.claude/knowledge/architecture.md`.
- Before writing new code in an area you have not touched this session, read `.claude/knowledge/conventions.md`.
- Before working on a feature, or choosing what to build next, find its owning modules and the roadmap in `.claude/knowledge/features.md`.

## Commands

```bash
dotnet build            # fails on missing XML docs or multi-type files
dotnet test
dotnet run --project samples/Platformer
```

Run `dotnet build` and `dotnet test` before committing; never commit failing tests.

## Git

Conventional Commits (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`); commit per phase or logical unit. Never add a Co-Authored-By trailer to commit messages.

## Team rules

<!-- manual:start -->
<!-- manual:end -->

## Keeping this context current

In any change that alters the data schema, adds, removes, or renames a project or module, moves a feature to different code, or makes an architecture decision, update CLAUDE.md and the files in `.claude/knowledge/` in the same change, without being asked. When the change records an architecture decision, write the decision and its reason into the knowledge files. Before finishing such a change, re-read the affected sections and confirm they still match the code. Stale context is a bug.
