param(
    [ValidateSet("Platformer", "Scene3D", "Arena3D")] [string]$Game = "Platformer",
    [string]$Configuration = "Debug",
    [string]$Project = "",
    [switch]$Hub,
    [string]$Shot = "",
    [int]$WaitSeconds = 5
)

# Starts the editor on a project: -Project <file.csgame.json>, or one of the samples with -Game (build first: dotnet build). With -Shot <png> it waits, screenshots the editor window and
# closes it, which is how the editor is checked without a person at the keyboard. Without it, the editor stays open until you close it.

$root = Split-Path -Parent $PSScriptRoot
Remove-Item (Join-Path $env:APPDATA "Game Editor\layout.ini") -ErrorAction SilentlyContinue # a fresh run builds the default window layout
$exe = Join-Path $root "src\Engine.Editor\bin\$Configuration\net10.0\Editor.exe"
if ($Hub) { $Project = "" } elseif ($Project -eq "") { $Project = Join-Path $root "samples\$Game\$Game.csgame.json" }
foreach ($path in @($exe, $(if ($Hub) { $exe } else { $Project }))) { if (-not (Test-Path $path)) { Write-Error "Missing $path (run dotnet build first)"; exit 1 } }

if ($Shot -eq "") {
    Start-Process -FilePath $exe -ArgumentList $(if ($Hub) { @() } else { @("`"$Project`"", "--no-build") }) -Wait
    return
}

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class EditorWin32 {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
}
"@

[EditorWin32]::SetProcessDPIAware() | Out-Null # without this the screenshot covers only 80% of a window on a 125% display
$proc = Start-Process -FilePath $exe -ArgumentList $(if ($Hub) { @() } else { @("`"$Project`"", "--no-build") }) -PassThru -RedirectStandardError "$Shot.err.txt"
try {
    Start-Sleep -Seconds $WaitSeconds
    $proc.Refresh()
    if ($proc.HasExited) { Write-Output "EXITED code=$($proc.ExitCode)"; Get-Content "$Shot.err.txt" | Select-Object -First 20; return }
    [EditorWin32]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 800
    $rect = New-Object EditorWin32+RECT
    [EditorWin32]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null
    $w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bmp.Size)
    $bmp.Save($Shot, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output "Captured ${w}x${h} -> $Shot"
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}
