# tools

PowerShell helpers for verifying the engine visually. Unit tests cover the maths; these cover what only a real GPU window can show.

| Script | What it does |
|---|---|
| `capture-window.ps1 -Exe <path> -Out <png> [-WaitSeconds 4]` | Launches the exe, waits, screenshots just its window, then kills it. Prints `EXITED code=...` and stderr if the game crashed on startup. |
| `drive-sample.ps1 -Exe <path> -OutDir <dir>` | Launches the sample and captures: `A_baseline.png`; a click in the level (`B_clicked.png`); `D` held (`C_holdD.png`); `Space` while running (`D_jumping.png`); F5 quick-save (`E_saved.png`); running left (`F_moved.png`); F9 quick-load (`G_loaded.png`, the hero's position should equal the saved one). Finally closes the window normally and prints the exit code, which must be 0 (a regression check for GL teardown). Coordinates and keys are hard-coded for the Platformer sample; edit them for other scenarios. |
| `run-editor.ps1 [-Game Platformer\|Scene3D] [-Shot <png>] [-WaitSeconds 5]` | Starts the standalone editor (`src/Engine.Editor`, `Editor.exe`) on a project (`-Project <file>`, or a sample with `-Game`; `-Hub` shows the hub; build first). With `-Shot` it waits, screenshots the window (DPI-aware, so the whole window) and closes it. Without it the editor stays open. |
| `drive-editor-app.ps1 -OutDir <dir>` | Drives the editor on the Platformer: default layout (`app_0_layout.png`), clicks a coin in the Scene panel (`app_1_selected.png`: the Inspector shows it), Play (`app_2_playing.png`) and Stop (`app_3_stopped.png`). Click positions are window-relative pixels for the default 1280x720 layout. |
| `drive-assets.ps1 -OutDir <dir>` | Drives the editor's Assets window on the Platformer: opens the Assets tab, expands `prefabs` (`assets_0_tree.png`), drags the coin prefab onto the Scene panel (`assets_1_dropped.png`: a new coin is placed under the mouse and selected) and undoes it with Ctrl+Z (`assets_2_undone.png`). Click positions are for the default layout. |
| `drive-hub.ps1 -OutDir <dir>` | Drives the hub end to end: New project, types a name and a temp location, Create, waits for the build and for the editor to open on the new project (`hub_0..3_*.png`). |
| `drive-project-menu.ps1 -OutDir <dir> -Project <file> [-Reload]` | Opens a project and drives the Project menu: Build (output in the Log tab), Validate assets, and with `-Reload` Build and reload game code (a second editor window appears, the first closes). |
| `drive-prefab.ps1 -OutDir <dir>` | Selects a coin, Edit > Save as prefab (free default name), saves, then tries a gizmo drag and Revert (the drag does not move the entity under synthetic input; see tasks). |
| `drive-demomap.ps1 -Exe <Platformer.exe> -OutDir <dir>` | Presses F11 in the Platformer (imports the Tiled demo map) and screenshots it twice, 0.6 s apart: the animated tile changes between the shots. |
| `drive-arena.ps1 -Exe <Arena3D.exe> -OutDir <dir>` | Plays a few seconds of Orb Run with held keys: start, walks left to collect an orb (HUD Orbs 1/12), jumps, then walks off the edge (`arena_0..3_*.png`). |
| `drive-inspector.ps1 -Exe <path> -OutDir <dir>` | Launches the sample, drags the Inspector window up from its default spot and clicks an entity in its list, saving `I1_moved.png` and `I2_selected.png` (the details pane should show the entity's components). Click coordinates assume a fresh layout, so delete `imgui.ini` first. |
| `capture-clean.ps1 -Exe <path> -Out <png> [-Keys 0x44] [-HoldSeconds 1] [-Then 0x0D] [-KeepUi]` | Launches the exe, hides the debug UI with `F1` (unless `-KeepUi`), optionally holds keys (virtual-key codes, comma separated) for `-HoldSeconds`, optionally taps one more key (`-Then`, e.g. Enter), then screenshots only the scene. Holding a harmless key such as `0x10` for 16 s lets the game-over screen appear. Use it to judge rendering effects (lighting, bloom) that the debug panels would cover. |
| (no script) hot-reload check | Run the sample, then overwrite `samples/Platformer/Assets/tiles.png` (for example with every colour inverted via `System.Drawing`) and watch the running game redraw its tiles within about a second; the console shows `Assets: Reloaded tiles.png`. Restore the file with git. |
| `drive-scene3d.ps1 -Exe <Scene3D.exe> -OutDir <dir>` | Launches the 3D sample, hides the panels (F1), throws three crates with Space (`S1_crates.png` after they land), left-clicks the floor to cast a ray (`S2_picked.png`: the glowing marker sits at the hit point), closes the window normally and prints the game's `Game:` log lines (including `Ray hit ...`) and the exit code (must be 0). |
| `drive-display.ps1 -Exe <Platformer.exe> -OutDir <dir>` | Launches the Platformer, hides the panels (F1) and screenshots each display mode: baseline, pixel-perfect (F6), HDR + tone map (F7), MSAA cycle (F8) in combination (`disp_0..5_*.png`). Reports an early exit (a GL error throws) and the exit code of a normal close. |
| `drive-shadows.ps1 -Exe <Platformer.exe> -OutDir <dir>` | Launches the Platformer, hides the panels (F1, with an Alt tap first so the window really has the keyboard focus) and screenshots the level with light shadows on (`shadow_on.png`) and off (`shadow_off.png`, F10). Shadows only show clearly when a light is near walls, so for a clear test add a `Light2D` entity and a `ShadowCaster2D` box next to the hero in the scene temporarily (revert with git). |
| `drive-scene3d-physics.ps1 -Exe <Scene3D.exe> -OutDir <dir>` | Launches the 3D sample, hides the panels, holds the arrow keys so the capsule player walks, throws crates with Space, jumps with Enter and saves `K0..K4` screenshots; prints the exit code and log. Arrow keys are extended keys, so the script sends the extended flag and real scan codes. |
| `dotnet run tools/make-arm.cs -- <out.glb>` | A .NET 10 file-based app that writes the 3D sample's skinned, animated two-bone arm (`samples/Scene3D/Assets/models/arm.glb`): a prism with joints, per-vertex weights and a "wave" clip, as a standard binary glTF. |
| `dotnet run tools/make-models.cs -- <out.glb>` | A .NET 10 file-based app that builds the 3D sample's house from `MeshBuilder` shapes and saves it as a binary glTF with `GltfWriter` (`samples/Scene3D/Assets/models/house.glb`). |
| `make-sounds.ps1 [-OutDir <dir>]` | Synthesises the sample's sound effects (`jump.wav`, `land.wav`, `spawn.wav`) into `samples/Platformer/Assets/sounds`. |
| `make-sprite-sheets.ps1 -OutDir <dir>` | Regenerates the Platformer sample's art in `samples/Platformer/Assets`: `tiles.png` (5 tiles), `hero.png` (8 animation frames), `creatures.png` (8 types x 2 walk frames) and `items.png` (spinning coin, locked and open exit door, checkpoint flag down and raised). |

The 3D sample works with the same scripts: `.\tools\capture-clean.ps1 -Exe samples\Scene3D.Launcher\bin\Debug\net10.0\Scene3D.exe -Out $env:TEMP\scene3d.png` (the F1 that hides the panels is occasionally missed on the first try; run it again). To check depth testing through the post-processing target, flip the vignette to `Enabled = true` in `Scene3DGame.Load` temporarily.

Example (after `dotnet build`):

```powershell
.\tools\capture-window.ps1 -Exe samples\Platformer.Launcher\bin\Debug\net10.0\Platformer.exe -Out $env:TEMP\shot.png
```

Then open the PNG. Keep screenshots out of the repo.

Added with the phase 23 batch (all take `-OutDir` and write numbered screenshots there):

| Script | What it checks |
|---|---|
| `drive-minimap.ps1 -Exe <Platformer.exe>` | the Platformer radar (a `RenderView` with its own colour grade), M to hide it, and F6 pixel-perfect |
| `drive-place3d.ps1` | Edit > Add 3D in the editor on Orb Run: the menu, a solid platform placed and selected, Ctrl+Z |
| `drive-assets-files.ps1` | the Assets window on a throwaway platformer project: thumbnail on hover, right-click menu, rename through the dialog (reports whether the file moved) |
| `drive-arena.ps1` | now also presses Escape for the pause menu (`arena_4_paused.png`) |
| `drive-hub.ps1` | now picks the 2D platformer template in the New project page |
| `drive-prefab.ps1` | Save as prefab, a gizmo drag and Revert to prefab (positions refreshed for the Windows 11 layout) |

A synthetic mouse wheel does not reach ImGui scroll areas here, so scripts avoid scrolling: expand folders so the file they need is visible, or use a project with fewer folders.
