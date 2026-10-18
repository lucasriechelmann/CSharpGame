# CSharpGame: a learning-oriented 2D game engine in C# (3D-ready)

Windows, .NET 10, Silk.NET (OpenGL 3.3 core), custom sparse-set ECS, Dear ImGui debug UI.
Goal: learn how engines work, so write core systems from scratch and use libraries only for plumbing.

## Layout

- `src/Engine.Core`     ECS, fixed timestep, math helpers. No GPU or window dependencies (unit-testable).
- `src/Engine.Platform` Silk.NET window, GL context, input, ImGui controller.
- `src/Engine.Rendering` Shader, Texture2D, SpriteBatch, Camera2D, Sprite component + SpriteRenderSystem. Pure maths (`SpriteQuad`, `Camera2D`) is unit-tested; GL classes need a context.
- `src/Engine.Runtime`  `Game` base class, loop, debug overlay. Ties Core and Platform together.
- `samples/Platformer`  The first game. References the engine like any game project would.
- `tests/`              xUnit tests. Core logic must be testable without a GPU.

Dependency direction: samples -> Runtime -> (Core, Platform); samples/Runtime -> Rendering -> Core. Core never references Platform, Rendering or Silk.NET.

## Rendering conventions

- World space: X right, Y up, Z = draw order (larger Z in front). Camera zoom 1 = 1 world unit per pixel.
- Texture/sprite space: (0,0) is the top-left; image row 0 is the top.
- `Matrix4x4` is row-vector/row-major; upload to GL **without transposing** and multiply as `uViewProjection * vec4(pos, 1)`.
- Sprites are drawn in submission order with alpha blending and no depth test; sort before submitting.
- `Game.Draw` clears/prepares the frame, then `RenderSystems` run, then the ImGui overlay. Create GPU resources in `Load`, dispose them in `Unload`.
- To check rendering visually, run the sample and capture its window (see the session scratchpad `capture.ps1` pattern: launch exe, `CopyFromScreen` the window rect).

## Input and assets conventions

- Game code asks for intent through `ActionMap` (`Actions.WasPressed(GameActions.Jump)`, `GetAxis(GameActions.MoveX)`), never for a physical key. Action names are `const string`s in a per-game `GameActions` class.
- `InputState` edges (pressed/released) last one frame. **Read input in per-frame code (`FrameSystems`/`Update`) and latch it into components**; fixed-step systems run 0..N times per frame and must not read edges.
- Loop order per frame: `FrameSystems` -> `Update` -> N x (`FixedSystems` -> `FixedUpdate`) -> `Input.EndFrame()`; then `Draw` -> `RenderSystems` -> ImGui.
- UI (ImGui) swallows key/mouse presses while it wants them; releases always pass through.
- Components reference assets by `AssetHandle<T>` (an int), never by the loaded object. Load with `Assets.Load<T>("relative/path.png")` (cached by normalised path) or `Assets.Add(name, obj)` for procedural assets. Save `Assets.NameOf(handle)`, not the id.
- `Game.Assets` owns and disposes everything it loads, so do not dispose assets yourself. Asset files live in the project's `Assets/` folder (copied next to the exe).

## Code rules (enforced where noted)

1. **One type per file.** Class, struct, record, interface, enum and delegate each get their own file, named after the type. *(Enforced: SA1402 is a build error.)*
2. **Document everything public.** XML `<summary>` on every public/protected type and member, plus any internal type whose purpose is not obvious. Say what it is for and why, not just its name. *(Enforced: CS1591 is a build error.)*
3. **Split big classes into partial files** named `Type.Topic.cs` (e.g. `World.Components.cs`) once a class passes roughly 150 lines or mixes topics. Prefer extracting a collaborator class when a *responsibility* is separable (see SRP); use partials when it is one responsibility that is merely long.
4. **SOLID.**
   - Single responsibility: one reason to change per class. If you need "and" to describe it, split it.
   - Open/closed: extend by adding systems, components and implementations, not by editing switch statements.
   - Liskov: overrides must honour the base contract (no surprise exceptions or no-ops).
   - Interface segregation: small, focused interfaces (`ISystem` has one method).
   - Dependency inversion: depend on abstractions at layer boundaries (rendering, audio, input, physics), not on Silk.NET types in game or Core code.
5. **Components are plain data** (structs, no behaviour). Behaviour lives in systems.
6. **3D-ready math.** Use `Vector3`/`Matrix4x4`/`Quaternion` for transforms (Z = depth/layer in 2D). No `Vector2`-only transforms.
7. **No allocation in hot paths** (per-frame/per-entity code): no LINQ, no closures that capture per frame, no `new` of reference types in update loops.
8. **Structural ECS changes during a query** go through a `CommandBuffer`.
9. **Fail loudly on misuse** (guard clauses with clear messages); never swallow exceptions.
10. **GPU resources are `IDisposable`** and owned by exactly one object.
11. File-scoped namespaces, nullable reference types on, no `#region`.

## Testing

- Every Core/Physics behaviour gets xUnit tests. Test names describe behaviour: `Destroyed_handle_goes_stale_even_after_slot_is_reused`.
- Use binary-exact floats (powers of two) in timing tests to avoid rounding flakiness.
- Tests may nest helper types only if tiny; shared fixtures go in `tests/*/Fixtures/`.

## Commands

```bash
dotnet build
dotnet test
dotnet run --project samples/Platformer
```

Run `dotnet build` before committing: it fails on rule 1 and 2 violations. Do not commit with failing tests.

## Git

Conventional Commits (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`). Commit per phase/logical unit.
Never add a Co-Authored-By trailer to commit messages.

## Roadmap (status)

1. Foundation: window, fixed-timestep loop, ImGui debug UI. **Done**
2. Sparse-set ECS with tests. **Done**
3. GL wrapper, shaders, textures, batched sprite renderer, 2D camera. **Done**
4. Input mapping, asset cache, sprite demo. **Done**
5. AABB/tile collision, platformer controller (coyote time, jump buffering).
6. Tilemaps and sprite animation.
7. JSON scenes and prefabs (design components to be serializable).
8. ImGui entity inspector/profiler, OpenAL audio.
9. Framebuffers, post-processing, 2D lights.
10. Playable platformer; then editor and 3D.
