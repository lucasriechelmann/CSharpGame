# tools

PowerShell helpers for verifying the engine visually. Unit tests cover the maths; these cover what only a real GPU window can show.

| Script | What it does |
|---|---|
| `capture-window.ps1 -Exe <path> -Out <png> [-WaitSeconds 4]` | Launches the exe, waits, screenshots just its window, then kills it. Prints `EXITED code=...` and stderr if the game crashed on startup. |
| `drive-sample.ps1 -Exe <path> -OutDir <dir>` | Launches the sample and captures: `A_baseline.png`; a click in the level (`B_clicked.png`); `D` held (`C_holdD.png`); `Space` while running (`D_jumping.png`); F5 quick-save (`E_saved.png`); running left (`F_moved.png`); F9 quick-load (`G_loaded.png`, the hero's position should equal the saved one). Finally closes the window normally and prints the exit code, which must be 0 (a regression check for GL teardown). Coordinates and keys are hard-coded for the Platformer sample; edit them for other scenarios. |
| `drive-inspector.ps1 -Exe <path> -OutDir <dir>` | Launches the sample, drags the Inspector window up from its default spot and clicks an entity in its list, saving `I1_moved.png` and `I2_selected.png` (the details pane should show the entity's components). Click coordinates assume a fresh layout, so delete `imgui.ini` first. |
| `make-sounds.ps1 [-OutDir <dir>]` | Synthesises the sample's sound effects (`jump.wav`, `land.wav`, `spawn.wav`) into `samples/Platformer/Assets/sounds`. |
| `make-sprite-sheets.ps1 -OutDir <dir>` | Regenerates the Platformer sample's art in `samples/Platformer/Assets`: `tiles.png` (5 tiles), `hero.png` (8 animation frames) and `creatures.png` (8 types x 2 walk frames). |

Example (after `dotnet build`):

```powershell
.\tools\capture-window.ps1 -Exe samples\Platformer\bin\Debug\net10.0\Platformer.exe -Out $env:TEMP\shot.png
```

Then open the PNG. Keep screenshots out of the repo.
