# CSharpGame

A small **2D game engine written in C#**, built to learn how engines work and designed so **3D can be added later**. The core systems (ECS, game loop, sprite batching, input mapping, asset management) are written from scratch; libraries are used only for plumbing such as windowing, OpenGL bindings, image decoding and the debug UI.

The first game it is being built for is a platformer.

> **Status:** work in progress. Phases 1 to 4 of 10 are done (see the [roadmap](#roadmap)).

## Features

- **Fixed-timestep game loop** with an interpolation factor, so simulation is deterministic and independent of frame rate
- **Custom sparse-set ECS**: generational entity handles, typed component pools, 1 to 3 component queries with `ref` access, deferred changes through a `CommandBuffer`, ordered system groups
- **Batched sprite renderer** (OpenGL 3.3 core): one draw call per texture run, tint, pivot, rotation, flip and sprite-sheet regions
- **2D camera** with zoom, rotation and screen-to-world conversion
- **Input mapping**: named actions and axes (`"Jump"`, `"MoveX"`) bound to keys and mouse buttons, with per-frame pressed/released edges
- **Asset manager** with typed integer handles, per-path caching and automatic disposal
- **Dear ImGui debug overlay** (FPS, fixed steps, entities, assets; toggle with `F1`) that games can extend with their own panels
- **3D-ready math**: transforms are `Vector3`/`Quaternion`; Z is draw order in 2D
- Unit-tested core (ECS, input, assets, timing, camera and sprite-quad maths)

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
dotnet run --project samples/Platformer
```

### The sample

The `Platformer` sample is a steerable hero among bouncing creatures, loaded from a generated sprite sheet.

| Input | Action |
|---|---|
| `W` `A` `S` `D` / arrow keys | Move the hero (the camera follows) |
| Left mouse button | Spawn 10 creatures at the cursor |
| Mouse wheel, or `Q` / `E` | Zoom out / in |
| `R` | Reset the camera |
| `F1` | Toggle the debug UI |

## Project layout

```
src/
  Engine.Core/        ECS, fixed timestep, Transform, input state + action map, asset manager (no GPU dependencies)
  Engine.Platform/    Silk.NET window, OpenGL context, input adapter, ImGui controller
  Engine.Rendering/   Shader, Texture2D, SpriteBatch, Camera2D, Sprite component, sprite render system
  Engine.Runtime/     Game base class and loop, debug overlay
samples/
  Platformer/         The first game, consuming the engine like any game project would
tests/                xUnit tests for Core and Rendering
tools/                PowerShell scripts to run, drive and screenshot the sample
```

Dependencies point one way: `samples -> Runtime -> (Core, Platform, Rendering)`, and `Platform`/`Rendering -> Core`. `Engine.Core` never references windowing, OpenGL or Silk.NET, which keeps the core logic unit-testable without a GPU.

## Writing a game

Subclass `Game`, bind your inputs, register systems, and override the hooks you need:

```csharp
public sealed class MyGame : Game
{
    public MyGame() : base("My Game")
    {
        Actions.BindAxis("MoveX", KeyCode.A, KeyCode.D);

        FrameSystems.Add(new PlayerInputSystem(Actions)); // read input once per frame
        FixedSystems.Add(new MovementSystem());           // simulate at a fixed rate
    }

    protected override void Load()
    {
        // GL is ready: create GPU resources, load assets, spawn entities.
        var texture = Assets.Load<Texture2D>("hero.png");
    }
}
```

The per-frame order is: frame systems, `Update`, then N fixed steps (fixed systems, `FixedUpdate`), then draw and render systems, then the ImGui overlay. Read input in per-frame code and latch it into components, because fixed steps can run zero or several times per frame.

## Design rules

The codebase follows a few strict conventions, several enforced by the compiler:

- One type per file (build error)
- XML documentation on all public members (build error)
- Large classes split into partial files by topic
- SOLID, with dependencies on abstractions at layer boundaries
- Components are plain data; behaviour lives in systems
- No allocations in per-frame hot paths

Details live in [`CLAUDE.md`](CLAUDE.md) and the [`.claude/knowledge/`](.claude/knowledge) folder (architecture, conventions and feature map).

## Roadmap

1. Foundation: window, fixed-timestep loop, ImGui debug UI (done)
2. Sparse-set ECS with tests (done)
3. GL wrapper, shaders, textures, batched sprite renderer, 2D camera (done)
4. Input mapping, asset cache, sprite demo (done)
5. AABB and tile collision, platformer controller (coyote time, jump buffering)
6. Tilemaps and sprite animation
7. JSON scenes and prefabs
8. ImGui entity inspector and profiler, OpenAL audio
9. Framebuffers, post-processing, 2D lights
10. Playable platformer, then an editor and 3D

## Built with

[.NET 10](https://dotnet.microsoft.com/) · [Silk.NET](https://github.com/dotnet/Silk.NET) (windowing, input, OpenGL) · [Dear ImGui](https://github.com/ocornut/imgui) via ImGui.NET · [StbImageSharp](https://github.com/StbSharp/StbImageSharp) · [xUnit](https://xunit.net/)

## License

No license has been chosen yet, so all rights are reserved by default. Add a `LICENSE` file to change that.
