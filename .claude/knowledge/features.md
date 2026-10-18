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
| Sample game | Steerable hero among bouncing creatures; click to spawn, scroll or Q/E to zoom, camera follows hero. | `samples/Platformer` | `PlatformerGame`, `Program.cs` |
| Visual verification | Scripts to launch, drive and screenshot the sample. | `tools/` | `tools/README.md` |

## Roadmap

Status of the phased plan (update when a phase lands or the plan changes):

1. Foundation: window, fixed-timestep loop, ImGui debug UI. **Done**
2. Sparse-set ECS with tests. **Done**
3. GL wrapper, shaders, textures, batched sprite renderer, 2D camera. **Done**
4. Input mapping, asset cache, sprite demo. **Done**
5. AABB/tile collision, own simple physics, platformer controller (coyote time, jump buffering), fixed-step rules.
6. Tilemaps (chunked, texture atlas) and sprite animation (clips, state machines).
7. JSON scenes and prefabs (System.Text.Json, components as serializable data, assets saved by name).
8. ImGui entity inspector/profiler, OpenAL audio.
9. Framebuffers, post-processing, 2D lights.
10. Playable platformer v1; then a full editor and 3D.

Known gaps to keep in mind: no render interpolation between fixed steps yet (needs a previous-transform component), no hot-reload of assets, no logging, high-DPI untested.
