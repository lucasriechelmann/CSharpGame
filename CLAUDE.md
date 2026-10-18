# CSharpGame: a learning-oriented 2D game engine in C# (3D-ready)

Windows, .NET 10, Silk.NET (OpenGL 3.3 core), custom sparse-set ECS, Dear ImGui debug UI.
Goal: learn how engines work, so write core systems from scratch and use libraries only for plumbing. First game: a platformer. Status: phases 1-4 of 10 done (see `.claude/knowledge/features.md`).

## Projects

- `src/Engine.Core`      ECS, fixed timestep, `Transform`, input state and action map, asset manager. No GPU or window dependencies; unit-testable.
- `src/Engine.Platform`  Silk.NET window, GL context, input adapter, ImGui controller.
- `src/Engine.Rendering` Shader, Texture2D, SpriteBatch, Camera2D, `Sprite` component, `SpriteRenderSystem`, `TextureLoader`.
- `src/Engine.Runtime`   `Game` base class and loop, debug overlay. Composition root: the only module allowed to depend on every engine module.
- `samples/Platformer`   The first game; references the engine like any game project.
- `tests/`               xUnit tests (`Engine.Core.Tests`, `Engine.Rendering.Tests`).
- `tools/`               PowerShell scripts to run, drive and screenshot the sample (see `tools/README.md`).

Dependency direction: samples -> Runtime -> (Core, Platform, Rendering); Rendering and Platform -> Core. Core never references Platform, Rendering or Silk.NET.

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
