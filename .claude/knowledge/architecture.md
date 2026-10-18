# Architecture

Learning-oriented 2D game engine in C# (.NET 10, Windows), designed so 3D can be added later. Goal: understand how engines work, so core systems are written from scratch and libraries are used only for plumbing (windowing, GL bindings, image decoding, debug UI).

## Projects and dependencies

```
samples/Platformer ──► Engine.Runtime ──► Engine.Rendering ──► Engine.Core
        │                   │                                     ▲
        │                   └──────────► Engine.Platform ─────────┤
        └──► Engine.Rendering, Engine.Core (direct, for components/types)
tests/Engine.Core.Tests ──► Engine.Core
tests/Engine.Rendering.Tests ──► Engine.Rendering
```

| Project | Role | Key packages |
|---|---|---|
| `src/Engine.Core` | GPU- and window-free logic: ECS, fixed timestep, `Transform`, input state and action map, asset manager. Fully unit-testable. | none |
| `src/Engine.Platform` | Silk.NET window, GL context, input adapter, ImGui controller. The only place that touches Silk windowing/input. | Silk.NET.Windowing/Input/OpenGL, Silk.NET.OpenGL.Extensions.ImGui |
| `src/Engine.Rendering` | GL wrappers and 2D rendering: `Shader`, `Texture2D`, `SpriteBatch`, `Camera2D`, `Sprite` component, `SpriteRenderSystem`, `TextureLoader`. Unsafe blocks allowed (GL pointer APIs only). | Silk.NET.OpenGL, StbImageSharp |
| `src/Engine.Runtime` | `Game` base class (the loop), `DebugOverlay`, `FrameStats`. **Composition root: the one module allowed to depend on every engine module** (decision: it auto-registers `TextureLoader` so games need no boilerplate; later Physics/Audio follow the same rule). | ImGui.NET via Silk ImGui extension |
| `samples/Platformer` | The first game; consumes the engine like any game project. | none beyond engine |

Rules: Core never references Platform, Rendering or Silk.NET. Layers depend on abstractions at their boundaries (input is `InputState`/`ActionMap`, not Silk types).

Shared build config is in `Directory.Build.props` (net10.0, nullable, implicit usings; for non-test projects XML docs + StyleCop `SA1402`, with `CS1591` and `SA1402` as errors). Analyzer severities are in `.editorconfig`.

## Frame data flow

Per display frame (`Game.OnUpdate` then `Game.OnRender`, driven by Silk's window loop):

1. Window events fill `InputState` (via `SilkInputAdapter`; key/mouse presses are dropped while ImGui wants the input, releases always pass).
2. `FrameSystems` run, then `Game.Update` (read input here, latch intent into components).
3. `FixedTimestep.Advance(dt)` returns N steps; for each: `FixedSystems`, then `Game.FixedUpdate`.
4. `Input.EndFrame()` clears pressed/released edges and scroll.
5. Render: `Game.Draw` (clear/prepare), `RenderSystems` (`SpriteRenderSystem` gathers `Transform`+`Sprite`, sorts back-to-front by Z then texture, feeds `SpriteBatch`), then the ImGui overlay (`DebugOverlay` + `Game.DebugGui`).

Shutdown: `Game.Dispose` runs `Unload()`, then `Assets.Dispose()` (disposes every loaded texture), then the window (GL context last).

## ECS (`src/Engine.Core/Ecs`)

- `Entity` = (Id, Generation); generation 0 is `Entity.Null`. `EntityAllocator` hands out ids and bumps generations on destroy so stale handles fail `IsAlive`.
- `World` (partial: `World.cs` lifetime, `.Components.cs`, `.Queries.cs`, `.Internals.cs`) owns one `ComponentPool<T>` per component type, stored in a `Dictionary<Type, IComponentPool>`.
- `ComponentPool<T>` is a sparse set: `_sparse[entityId] = denseIndex + 1`, packed `Dense`/`Components` arrays, swap-remove.
- Queries (`Query<T1>`, `<T1,T2>`, `<T1,T2,T3>`) pass components by `ref`; they take an iteration lock so `Create/Destroy/Add/Remove` throw mid-query. Use `CommandBuffer` to defer, `Set` to overwrite existing components safely.
- Systems implement `ISystem.Update(World, double dt)`; `SystemGroup` runs them in insertion order. `Game` exposes three groups: `FixedSystems`, `FrameSystems`, `RenderSystems`.

## Components (data)

| Component | Defined in | Notes |
|---|---|---|
| `Transform` | `src/Engine.Core/Components/Transform.cs` | `Vector3` position (Z = draw order), `Quaternion` rotation, `Vector3` scale. Never use `default`; use `Identity`/`At`. |
| `Sprite` | `src/Engine.Rendering/Sprite.cs` | `AssetHandle<Texture2D>`, size, pixel-space `Source` rect (empty = whole texture), tint, pivot, flip. |
| `Velocity`, `PlayerControlled`, `Bouncer` | `samples/Platformer/` | Game-specific; `Bouncer` is a tag. |

## Assets

`AssetManager` (Core) caches by normalised relative path per asset type, using registered `IAssetLoader<T>`s; `AssetHandle<T>` is a 1-based int id (0 = invalid) into a per-type `AssetStore<T>`. `Add(name, obj)` registers procedural assets. `Game.Assets` roots at `<exe dir>/Assets`; sample assets are copied there by `Platformer.csproj`. The manager owns and disposes `IDisposable` assets.

## Rendering pipeline

- World space: X right, Y up, Z draw order (larger = in front). `Camera2D` zoom 1 = 1 world unit per pixel; `GetViewProjection()` is row-vector `Matrix4x4`, uploaded to GL without transpose and used as `uViewProjection * vec4(pos, 1)`.
- `SpriteQuad.Write` (pure, tested) builds 4 vertices from a `SpriteDraw`; `SpriteBatch` fills a `SpriteVertex[]` (max 8192 sprites, 16-bit indices), uploads with `glBufferSubData`, draws with a static index buffer, flushing on texture change. Alpha blending on, depth test off. Built-in GLSL 330 shader in `SpriteShaderSource`.
- Texture space: (0,0) top-left, image row 0 is the top. Window framebuffer size is assumed equal to window size (inferred; not tested on high-DPI displays).

## External services

None (no network, database or config files).

## Planned (from the roadmap, not built yet)

Physics2D module (own AABB/tile collision), tilemaps and sprite animation, JSON scenes (System.Text.Json) with components as plain serializable data, ImGui inspector, OpenAL audio, post-processing/2D lights, an editor, and a 3D renderer beside `Engine.Rendering`.
