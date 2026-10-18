# Conventions

Each pattern names one real file as the reference example. No competing or legacy patterns exist yet; if one appears, record which is canonical here.

## Code organisation

- **One type per file**, named after the type (build error via StyleCop SA1402). Reference: `src/Engine.Core/Ecs/EntityAction.cs`, `EntityAction2.cs`, `EntityAction3.cs` (generic delegate overloads get a numeric suffix because filenames cannot carry the generic arity).
- **Partial classes by topic** once a class passes ~150 lines or mixes topics, named `Type.Topic.cs`, with a `// comment` header saying the topic. Reference: `src/Engine.Core/Ecs/World.Components.cs` (and `SpriteBatch.Gpu.cs` for the GL half of the batch).
- **Extract a collaborator when the responsibility is separable** (SRP). Reference: `EntityAllocator` out of `World`; `DebugOverlay` and `FrameStats` out of `Game`; `SilkInputAdapter` out of `GameWindow`.
- File-scoped namespaces, nullable on, no `#region`, primary constructors for small dependency-holders (`src/Engine.Runtime/DebugOverlay.cs`).

## Documentation

XML `<summary>` on every public/protected type and member (build error via CS1591), explaining purpose and why, plus `<param>` for non-obvious parameters and `<exception>` when guard clauses throw. Reference: `src/Engine.Core/Assets/AssetManager.cs`. Use `<inheritdoc />` for interface/override implementations. Test projects are exempt.

## ECS and gameplay code

- **Components are plain data structs**; behaviour lives in `ISystem`s. Reference: `samples/Platformer/Velocity.cs`, `samples/Platformer/MovementSystem.cs`. Tag components are empty structs (`Bouncer.cs`).
- **Static factories for components whose `default` is invalid** (`Transform.Identity`/`At`, `Sprite.Create`).
- **Queries use `ref` lambdas** with the `EntityAction` delegates. Systems that run every frame and need a capturing callback cache the delegate in a field to avoid per-frame allocation. Reference: `src/Engine.Rendering/SpriteRenderSystem.cs` (`_collect`).
- **Structural changes during a query go through `CommandBuffer`** (`src/Engine.Core/Ecs/CommandBuffer.cs`).
- **Cached query callbacks**: a system whose query callback needs per-run values keeps them in fields and a delegate created in the constructor (no per-frame closure). Reference: `samples/Platformer/PlayerInputSystem.cs`, `src/Engine.Physics2D/PhysicsSystem.cs`. Non-capturing lambdas are fine inline (`FaceVelocitySystem.cs`).
- **Systems that write what the next step reads** (controller to physics to controller via `PhysicsBody.Contacts`) rely on fixed order in `FixedSystems`; document the required order in the system's `<summary>`. Reference: `PlatformerControllerSystem.cs`.
- **Content as assets**: shared, non-trivial runtime objects (tile layers, tile sets, animation state machines) are registered in `Assets` under a path-like name and referenced from components by `AssetHandle<T>`. Reference: `samples/Platformer/PlatformerGame.Spawning.cs` (`SpawnTileLayers`).
- **Content is data, behaviour is code**: levels, prefabs, tile sets and animation graphs live as JSON under the game's `Assets/`; code loads them by name (`Scenes.LoadFile`, `Scenes.Instantiate`, `Assets.Load<T>`) and then varies instances through component refs. Reference: `samples/Platformer/PlatformerGame.Scene.cs` (`SpawnCreature` instantiates a prefab then tweaks it).
- **Making a component saveable**: public-field struct, a static factory if `default` is invalid, registered by name in the registry. Reference: `src/Engine.Runtime/EngineComponents.cs`.
- **Per-game id/name constants** live in a static class (`TileIds.cs`, `GameActions.cs`, parameter names on `HeroAnimations`); no magic numbers or strings at call sites.
- **Data-driven animation**: build a state machine in a small factory (`HeroAnimations.Create`), cache parameter indices in the system constructor, write parameters per frame. Reference: `samples/Platformer/HeroAnimationSystem.cs`.
- **System placement:** per-frame/input systems in `FrameSystems`, simulation in `FixedSystems`, drawing in `RenderSystems`. Reference: `samples/Platformer/PlatformerGame.cs` constructor.
- **Input:** read via `ActionMap` using `const string` names in a per-game `GameActions` class; read edges per frame and latch into components, never inside fixed systems. Reference: `samples/Platformer/PlayerInputSystem.cs`.
- **Full-screen effects:** derive from `SinglePassEffect` (fragment source as a `const string`, settings as public auto-properties, upload them in `SetUniforms`) and add it with `PostProcess.Add` in `Load`. Reference: `src/Engine.Rendering/PostProcessing/VignetteEffect.cs`. Effects that need several passes implement `IPostEffect` (`BloomEffect`).
- **Game rules versus detection:** rules in a pure, unit-tested class; a system detects and reports. Tests build a tiny `World` and a real system with `NullAudioDevice` (`tests/Platformer.Tests/Fixtures/InteractionWorld.cs`).
- **Timing your own work:** register a profiler section once (`Profiler.GetSection`) and keep the id; wrap the work in `Begin`/`End`. Reference: `src/Engine.Runtime/Game.cs` (`_updateSection`).
- **Sound from gameplay:** observe state (physics contacts, velocity) in a frame system and call `IAudioDevice.Play`; preload clips in the constructor or `Load`. Reference: `samples/Platformer/HeroSoundSystem.cs`.
- **Assets:** components hold `AssetHandle<T>`, load via `Game.Assets`; do not dispose assets manually.

## Errors and validation

Fail loudly with guard clauses and specific exceptions and messages naming the offending value (`InvalidOperationException`, `ArgumentException`, `KeyNotFoundException`, `FileNotFoundException`); never swallow exceptions. Reference: `src/Engine.Core/Ecs/World.Internals.cs` (`ThrowIfDead`, `ThrowIfIterating`), `src/Engine.Core/Input/ActionMap.cs` (unbound action throws to surface typos). No logging framework yet.

## Math and rendering

- Transforms are 3D (`Vector3`, `Quaternion`, `Matrix4x4`); Z is draw order in 2D. Reference: `src/Engine.Core/Components/Transform.cs`.
- Put pure maths in testable static/pure types separate from GL calls. Reference: `src/Engine.Rendering/SpriteQuad.cs` vs `SpriteBatch.Gpu.cs`.
- GPU objects are `IDisposable`, owned by exactly one object; `unsafe` only where GL needs raw pointers (`Engine.Rendering` has `AllowUnsafeBlocks`).
- No allocation in per-frame/per-entity paths (no LINQ, no per-frame capturing closures).

## Tests (xUnit v3)

- Test projects use `xunit.v3` with `<OutputType>Exe</OutputType>` and run on Microsoft.Testing.Platform (opted in by the root `global.json`, because the .NET 10 SDK no longer supports the VSTest target for it). Do not add `Microsoft.NET.Test.Sdk` or `xunit.runner.visualstudio`; new test projects copy an existing `.csproj`.
- Test names describe behaviour with underscores. Reference: `tests/Engine.Core.Tests/Ecs/WorldEntityTests.cs`.
- One test class per topic, shared fixtures in `tests/*/Fixtures/` (own files). Reference: `tests/Engine.Core.Tests/Fixtures/`.
- Timing/float tests use binary-exact values (powers of two) to avoid rounding flakiness. Reference: `tests/Engine.Core.Tests/FixedTimestepTests.cs`.
- Simulation tests (physics, controller) run the real systems in a small fixture level at a power-of-two timestep and assert behaviour over steps. Reference: `tests/Engine.Physics2D.Tests/Fixtures/PlatformerSim.cs`.
- JSON/format tests cover: happy path, round trip (write then read), defaults when keys are omitted, and every rejection path with the exception type (`InvalidOperationException` for bad data, `FileNotFoundException` for missing files). Reference: `tests/Engine.Scenes.Tests/SceneLoadFailureTests.cs` (also asserts the world is unchanged on failure).
- Filesystem tests use a unique temp directory and clean up in `Dispose`. Reference: `tests/Engine.Core.Tests/Assets/AssetManagerTests.cs`.
- Audio device tests talk to real OpenAL and return quietly when no device exists; keep them silent (`volume: 0`). Pure parts (`WavDecoder`, `VoicePool`) are tested with in-memory data (`tests/Engine.Audio.Tests/Fixtures/WavBuilder.cs`). Profiler tests wind a `FakeClock` by hand.
- Rendering maths that feeds GPU code is pure and tested (`LightMath`: falloff, gradient pixels, culling); render targets, effects and `LightingEffect` are verified in the window with `tools/capture-clean.ps1`.
- GPU code (`Shader`, `Texture2D`, `SpriteBatch`, `RenderTarget`, post effects) and ImGui panels (`FieldEditor`, `EntityInspector`, `ProfilerPanel`) have no unit tests; verify it visually with `tools/` (see `tools/README.md`).

## Git

Conventional Commits (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`), one commit per phase or logical unit, no Co-Authored-By trailer. Run `dotnet build` and `dotnet test` before committing.
