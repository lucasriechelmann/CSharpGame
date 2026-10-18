# tools

PowerShell helpers for verifying the engine visually. Unit tests cover the maths; these cover what only a real GPU window can show.

| Script | What it does |
|---|---|
| `capture-window.ps1 -Exe <path> -Out <png> [-WaitSeconds 4]` | Launches the exe, waits, screenshots just its window, then kills it. Prints `EXITED code=...` and stderr if the game crashed on startup. |
| `drive-sample.ps1 -Exe <path> -OutDir <dir>` | Launches the sample, then captures `A_baseline.png`, clicks in the level (`B_clicked.png`), holds `D` for 1 s (`C_holdD.png`) and presses `Space` while running (`D_jumping.png`). Coordinates and keys are hard-coded for the Platformer sample; edit them for other scenarios. |
| `make-sprite-sheets.ps1 -OutDir <dir>` | Regenerates the Platformer sample's art in `samples/Platformer/Assets`: `tiles.png` (5 tiles), `hero.png` (8 animation frames) and `creatures.png` (8 types x 2 walk frames). |

Example (after `dotnet build`):

```powershell
.\tools\capture-window.ps1 -Exe samples\Platformer\bin\Debug\net10.0\Platformer.exe -Out $env:TEMP\shot.png
```

Then open the PNG. Keep screenshots out of the repo.
