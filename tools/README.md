# tools

PowerShell helpers for verifying the engine visually. Unit tests cover the maths; these cover what only a real GPU window can show.

| Script | What it does |
|---|---|
| `capture-window.ps1 -Exe <path> -Out <png> [-WaitSeconds 4]` | Launches the exe, waits, screenshots just its window, then kills it. Prints `EXITED code=...` and stderr if the game crashed on startup. |
| `drive-sample.ps1 -Exe <path> -OutDir <dir>` | Launches the sample and captures: `A_baseline.png`; a click in the level (`B_clicked.png`); `D` held (`C_holdD.png`); `Space` while running (`D_jumping.png`); F5 quick-save (`E_saved.png`); running left (`F_moved.png`); F9 quick-load (`G_loaded.png`, the hero's position should equal the saved one). Finally closes the window normally and prints the exit code, which must be 0 (a regression check for GL teardown). Coordinates and keys are hard-coded for the Platformer sample; edit them for other scenarios. |
| `drive-inspector.ps1 -Exe <path> -OutDir <dir>` | Launches the sample, drags the Inspector window up from its default spot and clicks an entity in its list, saving `I1_moved.png` and `I2_selected.png` (the details pane should show the entity's components). Click coordinates assume a fresh layout, so delete `imgui.ini` first. |
| `capture-clean.ps1 -Exe <path> -Out <png> [-Keys 0x44] [-HoldSeconds 1] [-Then 0x0D] [-KeepUi]` | Launches the exe, hides the debug UI with `F1` (unless `-KeepUi`), optionally holds keys (virtual-key codes, comma separated) for `-HoldSeconds`, optionally taps one more key (`-Then`, e.g. Enter), then screenshots only the scene. Holding a harmless key such as `0x10` for 16 s lets the game-over screen appear. Use it to judge rendering effects (lighting, bloom) that the debug panels would cover. |
| `drive-editor.ps1 -Exe <path> -OutDir <dir> [-Save]` | Launches the sample, hides the debug panels (F1), turns the scene editor on (F2), clicks the hero (select), drags its X arrow, undoes and redoes the move (Ctrl+Z, Ctrl+Y; `E3u`/`E3r` shots), wheel-zooms and middle-drag pans, saving `E1..E4` screenshots and the game's stdout to `out.txt`. Click coordinates were measured on a 125% scaled display (`SetCursorPos` takes DPI-virtualised pixels); recalibrate them on other scales. `-Save` sends Ctrl+S, which overwrites the project's `Assets/scenes/level.scene.json`: revert it with git. |
| `make-sounds.ps1 [-OutDir <dir>]` | Synthesises the sample's sound effects (`jump.wav`, `land.wav`, `spawn.wav`) into `samples/Platformer/Assets/sounds`. |
| `make-sprite-sheets.ps1 -OutDir <dir>` | Regenerates the Platformer sample's art in `samples/Platformer/Assets`: `tiles.png` (5 tiles), `hero.png` (8 animation frames), `creatures.png` (8 types x 2 walk frames) and `items.png` (spinning coin, locked and open exit door, checkpoint flag down and raised). |

Example (after `dotnet build`):

```powershell
.\tools\capture-window.ps1 -Exe samples\Platformer\bin\Debug\net10.0\Platformer.exe -Out $env:TEMP\shot.png
```

Then open the PNG. Keep screenshots out of the repo.
