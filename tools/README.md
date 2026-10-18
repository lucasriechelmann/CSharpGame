# tools

PowerShell helpers for verifying the engine visually. Unit tests cover the maths; these cover what only a real GPU window can show.

| Script | What it does |
|---|---|
| `capture-window.ps1 -Exe <path> -Out <png> [-WaitSeconds 4]` | Launches the exe, waits, screenshots just its window, then kills it. Prints `EXITED code=...` and stderr if the game crashed on startup. |
| `drive-sample.ps1 -Exe <path> -OutDir <dir>` | Launches the sample, then captures `A_baseline.png`, clicks in the arena (`B_clicked.png`) and holds `D` for 1.5 s (`C_holdD.png`). Coordinates and key are hard-coded for the Platformer sample; edit them for other scenarios. |
| `make-creature-sheet.ps1 -Out <png>` | Regenerates `samples/Platformer/Assets/creatures.png` (8 creatures, 16x16 cells, hero with crown at cell 0). |

Example (after `dotnet build`):

```powershell
.\tools\capture-window.ps1 -Exe samples\Platformer\bin\Debug\net10.0\Platformer.exe -Out $env:TEMP\shot.png
```

Then open the PNG. Keep screenshots out of the repo.
