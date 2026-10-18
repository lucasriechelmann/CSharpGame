param([string]$OutDir, [string]$Game = "Platformer", [string]$Configuration = "Debug")

# Drives the standalone editor on the Platformer: screenshots the default layout (app_0_layout.png), clicks a coin in the scene to select it
# (app_1_selected.png: the Inspector shows it), presses Play (app_2_playing.png) and Stop (app_3_stopped.png). Screen positions are
# window-relative pixels read off a screenshot of the default layout at 1280x720, so the script assumes that window size.

$root = Split-Path -Parent $PSScriptRoot
Remove-Item (Join-Path $env:APPDATA "Game Editor\layout.ini") -ErrorAction SilentlyContinue # a fresh run builds the default window layout
$exe = Join-Path $root "src\Engine.Editor\bin\$Configuration\net10.0\Editor.exe"
$project = Join-Path $root "samples\$Game\$Game.csgame.json"

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class EdDrive {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
}
"@
[EdDrive]::SetProcessDPIAware() | Out-Null

function Shot($proc, $path) {
    $rect = New-Object EdDrive+RECT
    [EdDrive]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null
    $w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bmp.Size)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output "shot $path (${w}x${h})"
}

function Click($proc, $x, $y) {
    $rect = New-Object EdDrive+RECT
    [EdDrive]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null
    [EdDrive]::SetCursorPos($rect.Left + $x, $rect.Top + $y) | Out-Null
    Start-Sleep -Milliseconds 400
    [EdDrive]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100
    [EdDrive]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 700
}

$proc = Start-Process -FilePath $exe -ArgumentList @("`"$project`"", "--no-build") -PassThru -RedirectStandardError "$OutDir\app_err.txt"
$null = $proc.Handle
try {
    Start-Sleep -Seconds 6
    $proc.Refresh()
    if ($proc.HasExited) { Write-Output "EXITED code=$($proc.ExitCode)"; Get-Content "$OutDir\app_err.txt" | Select-Object -First 20; return }
    [EdDrive]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero); [EdDrive]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero)
    [EdDrive]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 800

    Shot $proc "$OutDir\app_0_layout.png"
    Click $proc 872 165    # a coin in the scene
    Shot $proc "$OutDir\app_1_selected.png"
    Click $proc 265 89     # Play
    Start-Sleep -Milliseconds 1500
    Shot $proc "$OutDir\app_2_playing.png"
    Click $proc 265 89     # Stop
    Shot $proc "$OutDir\app_3_stopped.png"

    if ($proc.HasExited) { Write-Output "EXITED during the run (code $($proc.ExitCode))"; Get-Content "$OutDir\app_err.txt" | Select-Object -First 20 }
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}
