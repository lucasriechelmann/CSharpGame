# CSharpGame

A small **game engine written in C#**, built to learn how engines work. It started as a 2D engine and now also does 3D. The core systems (ECS, game loop, sprite batching, physics, input mapping, asset management, the editor) are written from scratch; libraries are used only for plumbing such as windowing, OpenGL bindings, image decoding and the debug UI.

It ships two playable games (a 2D platformer and a 3D orb-collecting game) and a **standalone editor** that can create, edit, build and publish game projects.

> **Status:** phases 1 to 23 are done (see the [roadmap](#roadmap)). What is still open is listed in [`.claude/knowledge/tasks.md`](.claude/knowledge/tasks.md).

## Features

### Core and simulation

- **Fixed-timestep game loop** with an interpolation factor, so simulation is deterministic and independent of frame rate; moving entities are drawn between their last two steps
- **Custom sparse-set ECS**: generational entity handles, typed component pools, 1 to 3 component queries with `ref` access, deferred changes through a `CommandBuffer`, ordered system groups, allocation-free lookups (`FindByName`, `FindFirst<T>`, `CountWith<T>`)
- **Transform hierarchy**: a `Parent` component gives an entity a local transform; world matrices come from the parent chain
- **Input mapping**: named actions and axes bound to keys and mouse buttons with per-frame edges; **players can rebind keys** in `game.settings.json` (a typo in a key name fails loudly at start-up)
- **Asset manager** with typed handles, per-path caching, hot-reload and automatic disposal
- **Scenes and prefabs**: the whole world saved and loaded as JSON; prefabs with merge-style overrides; assets referenced by name; loading is all-or-nothing
- **Game settings**: `game.settings.json` next to a shipped game sets window size, full screen, vsync, volume and key bindings

### 2D

- **Batched sprite renderer** (OpenGL 3.3 core), **2D camera**, sprite sheets and a data-driven **animation state machine**
- **2D physics and platformer feel**: swept collision (no tunnelling), moving platforms that carry riders, coyote time, jump buffering, variable jump
- **Tilemaps**: chunked tile layers drawn from cached GPU chunk meshes, flip flags, animated tiles, parallax, collision derived from the same layer, **Tiled `.tmj` import** and a **`TiledObjectSpawner`** that turns Tiled objects into prefab instances
- **Lighting and effects**: `Light2D` point lights with shadows cast by walls, bloom, vignette, colour grade, HDR with tone mapping, MSAA, pixel-perfect scaling
- **Render views**: `RenderView` draws a second picture of the world (a minimap, a mirror) with its own camera and its own effect chain

### 3D

- **Meshes, PBR materials** (metallic-roughness, maps, alpha modes), **glTF/glb models** with skins and animation, instanced draws, sorted transparency, frustum culling
- **Lights and shadows**: a directional sun with a shadow map, point lights, and a **procedural sky** with a sun disc
- **Own 3D physics**: swept boxes, capsules and spheres, body-vs-body pushing and stacking, a character controller, oriented ramps, raycasts
- **Cameras**: fly camera, orbit camera, and third-person follow with wall collision

### Audio

- **OpenAL audio**: WAV clips, a voice pool, **mixer groups**, positioned 3D sounds (`AudioSource` and `AudioListener`), **streamed music**, silent fallback when no device exists

### Tools

- **Standalone editor** (`Editor.exe`) with a Windows 11 look: hierarchy, inspector, undo/redo with a History window, gizmos (move, rotate, scale), 2D and 3D scene views, tile painting, **Edit > Add 3D** for placing boxes, solid platforms and lights, an **Assets window** (thumbnails, drag a prefab into the scene, rename, delete, new folder), and an audio mixer panel
- **Project hub**: create a game from a template (**empty 2D, 2D platformer, 3D scene**), open or revisit projects
- **Build and publish**: build the game from the editor, validate assets, publish a self-contained Windows build
- **Debug UI** (`F1`): FPS, entity inspector, profiler with a frame graph, live post-effect tuning, log window; games add their own panels
- **Pause menu helper** shared by 2D and 3D games, and `Game.Hud()` for player-facing UI

## Requirements

- Windows (the only platform targeted for now)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A GPU and driver with OpenGL 3.3 support

## Getting started

```bash
git clone https://github.com/lucasriechelmann/CSharpGame.git
cd CSharpGame
dotnet build
dotnet test
dotnet run --project samples/Platformer.Launcher   # the 2D game
dotnet run --project samples/Arena3D.Launcher      # the 3D game
```

Open the editor on a sample (build first):

```powershell
.\tools\run-editor.ps1 -Game Platformer    # or Scene3D, Arena3D
.\tools\run-editor.ps1 -Hub                # the project hub: new project, open, recent
```

### Make your own game

In the hub choose **New project**, pick a template and press Create: the editor builds it and opens it. From the command line:

```bash
Editor.exe --new <folder> <name> --template platformer   # empty | platformer | 3d
Editor.exe --publish <project.csgame.json> [output folder]
```

A project is a folder with a `.csgame.json` file, an `Assets` folder, a `Game` library (your code) and a `Launcher` (the program you ship).

### The samples

**Platformer** (`samples/Platformer`): collect all 25 coins to open the exit door, then reach it. Touching a creature costs a life, landing on one from above stomps it, and finishing fast earns a time bonus. It is almost entirely data (scene, prefabs, tile layers, tile set and animation graphs are JSON), with lights, bloom, a vignette and a radar in the corner that is a second camera with its own colour grade.

| Input | Action |
|---|---|
| `A` `D` / arrows | Run (the camera follows) |
| `Space` / `W` / up arrow | Jump (hold for a higher jump) |
| Left mouse button | Spawn 5 creatures at the cursor |
| Mouse wheel, or `Q` / `E` | Zoom out / in |
| `M` | Show or hide the radar |
| `Esc` / `P` | Pause menu |
| `F5` / `F9` | Quick-save / quick-load the world |
| `F1` | Toggle the debug UI |

**Orb Run** (`samples/Arena3D`): collect 12 glowing orbs on stepped platforms before the 90 second clock runs out; falling off the edge costs 5 seconds. Third-person camera that does not clip through walls, a player that turns to face its walk, music, a positional hum from a lamp, and a procedural sky. `W` `A` `S` `D` walk, `Space` jumps, the mouse turns the camera, `Esc` pauses, `Enter` plays again.

**Scene3D** (`samples/Scene3D`): a lit room loaded from JSON with a glTF house, an animated arm, point lamps and a fly camera.

All three open in the editor (F2 inside a game, or through `tools/run-editor.ps1`).

## Project layout

```
src/
  Engine.Core/        ECS, fixed timestep, Transform and hierarchy, input state + action map, asset manager (no GPU dependencies)
  Engine.Platform/    Silk.NET window, OpenGL context, input adapter, ImGui controller
  Engine.Rendering/   Shader, Texture2D, SpriteBatch, Camera2D, render targets, post-processing, 2D lights and shadows
  Engine.Rendering3D/ Meshes, PBR materials, glTF models, cameras, lights, shadow map, sky, mesh render system
  Engine.Physics2D/   AABB, tile grid, swept collision, physics system, platformer controller
  Engine.Physics3D/   Swept boxes, capsules, oriented boxes, raycasts, character controller
  Engine.Tilemaps/    Chunked tile layers, tile sets, cached chunk meshes, Tiled import, layer-to-collision builder
  Engine.Animation/   Animation clips, state machine, animator component and system
  Engine.Scenes/      JSON scenes and prefabs: component registry, scene serializer, merge overrides
  Engine.Audio/       WAV decoding and streaming, mixer groups, positioned sounds, OpenAL device (and a silent fallback)
  Engine.Runtime/     Game base class and loop, debug overlay, inspector, profiler, render views, pause menu, key rebinding, editor seam
  Engine.Editor/      The standalone editor program: hub, projects and templates, scene editor, gizmos, assets window
samples/
  Platformer/         The 2D game (library) and Platformer.Launcher (the executable that ships)
  Arena3D/            Orb Run, the 3D game, and Arena3D.Launcher
  Scene3D/            The 3D showcase room, and Scene3D.Launcher
tests/                xUnit tests, one project per module
tools/                PowerShell scripts to run, drive and screenshot the games and the editor (see tools/README.md)
```

Dependencies point one way: `launchers -> samples -> Runtime -> (every engine module)`; the `Editor` depends on `Runtime` and nothing depends on the editor. `Tilemaps -> (Rendering, Physics2D)`, `Animation -> Rendering`, `Rendering3D -> Rendering`, and everything else `-> Core`. `Runtime` is the only engine module that knows about all the others. `Engine.Core`, `Engine.Physics2D` and `Engine.Physics3D` never reference windowing, OpenGL or Silk.NET, so the simulation and content logic is unit-testable without a GPU.

## How a frame runs

```
OnUpdate:  frame systems -> Update -> N fixed steps (fixed systems, FixedUpdate) -> clear input edges
OnRender:  render views -> BeginScene -> Draw -> render systems -> EndScene (post effects)
OnGui:     Hud -> debug overlay -> editor
```

Read input in per-frame code and latch it into components, because fixed steps can run zero or several times per frame. Anything that follows a moving entity at render time (a camera, a light) must use the interpolated position and do it in `Draw`.

## Writing a game

Subclass `Game`, bind your inputs, register systems, and override the hooks you need:

```csharp
public sealed class MyGame : Game
{
    public MyGame(GameLaunch? launch = null) : base("My Game", launch: launch)
    {
        Actions.BindAxis("MoveX", KeyCode.A, KeyCode.D);

        FrameSystems.Add(new PlayerInputSystem(Actions)); // read input once per frame
        FixedSystems.Add(new MovementSystem());           // simulate at a fixed rate
    }

    protected override void Load()
    {
        // GL is ready: create GPU resources, load assets, spawn entities.
        Scenes.LoadFile("scenes/main.scene.json");
        var player = World.RequireByName("Player");
    }
}
```

Expose it with an `IGameModule`; the editor and the launcher both start the game through it. To rebind keys without rebuilding, a player edits `game.settings.json`:

```json
{ "bindings": { "Jump": ["Space", "Mouse.Right"] }, "axisBindings": { "MoveX": ["Left:Right"] } }
```

## Design rules

The codebase follows a few strict conventions, several enforced by the compiler:

- One type per file (build error)
- XML documentation on all public members (build error)
- Large classes split into partial files by topic
- SOLID, with dependencies on abstractions at layer boundaries
- Components are plain data; behaviour lives in systems
- No allocations in per-frame hot paths

Details live in [`CLAUDE.md`](CLAUDE.md) and the [`.claude/knowledge/`](.claude/knowledge) folder (architecture, conventions, feature map and the task backlog).

## Roadmap

1. Foundation: window, fixed-timestep loop, ImGui debug UI (done)
2. Sparse-set ECS with tests (done)
3. GL wrapper, shaders, textures, batched sprite renderer, 2D camera (done)
4. Input mapping, asset cache, sprite demo (done)
5. AABB and tile collision, platformer controller (done)
6. Tilemaps and sprite animation (done)
7. JSON scenes and prefabs (done)
8. ImGui entity inspector and profiler, OpenAL audio (done)
9. Framebuffers, post-processing, 2D lights (done)
10. Playable platformer v1 (done)
11. Polish pass (done)
12. In-game scene editor (done, later moved out of the game)
13. 3D foundation: meshes, materials, cameras, lights, glTF models, box physics (done)
14. Transform hierarchy (done)
15. 3D editor view and gizmos (done)
16. 3D physics stage 2: body-vs-body, capsules, character controller, ramps (done)
17. 3D rendering depth: PBR, shadows, instancing, transparency, glTF animation (done)
18. Content pipeline: tile flip flags, animated tiles, parallax, Tiled import, tile brushes, History window, prefab tools (done)
19. HDR, MSAA, pixel-perfect, 2D light shadows, audio mixer, positioned sound, streaming (done)
20. Verify and finish (done)
21. A standalone editor with a Windows 11 look (done)
22. Projects, the hub, build and publish (done)
23. Orb Run, render views, procedural sky, key rebinding, project templates, Add 3D, asset file operations (done)

Open ideas (soft light shadows, polygon shadow casters, image-based lighting, 3D rigid-body rotation, Ogg/MP3 audio, a second Tiled level) are in [`tasks.md`](.claude/knowledge/tasks.md).

## Built with

[.NET 10](https://dotnet.microsoft.com/) · [Silk.NET](https://github.com/dotnet/Silk.NET) (windowing, input, OpenGL) · [OpenAL Soft](https://openal-soft.org/) via Silk.NET (audio) · [Dear ImGui](https://github.com/ocornut/imgui) via ImGui.NET · [StbImageSharp](https://github.com/StbSharp/StbImageSharp) · [xUnit v3](https://xunit.net/)

## License

Released under the [MIT License](LICENSE).
