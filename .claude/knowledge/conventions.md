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
- **System placement:** per-frame/input systems in `FrameSystems`, simulation in `FixedSystems`, drawing in `RenderSystems`. Reference: `samples/Platformer/PlatformerGame.cs` constructor.
- **Input:** read via `ActionMap` using `const string` names in a per-game `GameActions` class; read edges per frame and latch into components, never inside fixed systems. Reference: `samples/Platformer/PlayerInputSystem.cs`.
- **Assets:** components hold `AssetHandle<T>`, load via `Game.Assets`; do not dispose assets manually.

## Errors and validation

Fail loudly with guard clauses and specific exceptions and messages naming the offending value (`InvalidOperationException`, `ArgumentException`, `KeyNotFoundException`, `FileNotFoundException`); never swallow exceptions. Reference: `src/Engine.Core/Ecs/World.Internals.cs` (`ThrowIfDead`, `ThrowIfIterating`), `src/Engine.Core/Input/ActionMap.cs` (unbound action throws to surface typos). No logging framework yet.

## Math and rendering

- Transforms are 3D (`Vector3`, `Quaternion`, `Matrix4x4`); Z is draw order in 2D. Reference: `src/Engine.Core/Components/Transform.cs`.
- Put pure maths in testable static/pure types separate from GL calls. Reference: `src/Engine.Rendering/SpriteQuad.cs` vs `SpriteBatch.Gpu.cs`.
- GPU objects are `IDisposable`, owned by exactly one object; `unsafe` only where GL needs raw pointers (`Engine.Rendering` has `AllowUnsafeBlocks`).
- No allocation in per-frame/per-entity paths (no LINQ, no per-frame capturing closures).

## Tests (xUnit)

- Test names describe behaviour with underscores. Reference: `tests/Engine.Core.Tests/Ecs/WorldEntityTests.cs`.
- One test class per topic, shared fixtures in `tests/*/Fixtures/` (own files). Reference: `tests/Engine.Core.Tests/Fixtures/`.
- Timing/float tests use binary-exact values (powers of two) to avoid rounding flakiness. Reference: `tests/Engine.Core.Tests/FixedTimestepTests.cs`.
- Simulation tests (physics, controller) run the real systems in a small fixture level at a power-of-two timestep and assert behaviour over steps. Reference: `tests/Engine.Physics2D.Tests/Fixtures/PlatformerSim.cs`.
- Filesystem tests use a unique temp directory and clean up in `Dispose`. Reference: `tests/Engine.Core.Tests/Assets/AssetManagerTests.cs`.
- GPU code (`Shader`, `Texture2D`, `SpriteBatch`) has no unit tests; verify it visually with `tools/` (see `tools/README.md`).

## Git

Conventional Commits (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`), one commit per phase or logical unit, no Co-Authored-By trailer. Run `dotnet build` and `dotnet test` before committing.
