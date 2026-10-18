param([string]$OutDir, [string]$Configuration = "Debug")

# Drives the editor's Assets window on the Platformer: opens the Assets tab, expands the prefabs folder (assets_0_tree.png), drags the coin
# prefab onto the Scene panel (assets_1_dropped.png: a new coin is placed and selected) and undoes it with Ctrl+Z (assets_2_undone.png).
# Positions are window-relative pixels for the default 1280x720 layout.

$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "src\Engine.Editor\bin\$Configuration\net10.0\Editor.exe"
$dll = Join-Path $root "samples\Platformer\bin\$Configuration\net10.0\Platformer.Game.dll"
$project = Join-Path $root "samples\Platformer"

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class AssetsDrive {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
}
"@
[AssetsDrive]::SetProcessDPIAware() | Out-Null

function WindowRect($proc) { $r = New-Object AssetsDrive+RECT; [AssetsDrive]::GetWindowRect($proc.MainWindowHandle, [ref]$r) | Out-Null; return $r }
function Shot($proc, $path) {
    if ($proc.HasExited) { Write-Output "EXITED (code $($proc.ExitCode)) before $path"; return }
    $r = WindowRect $proc; $w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    [System.Drawing.Graphics]::FromImage($bmp).CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output "shot $path"
}
function MoveTo($proc, $x, $y) { $r = WindowRect $proc; [AssetsDrive]::SetCursorPos($r.Left + $x, $r.Top + $y) | Out-Null; Start-Sleep -Milliseconds 120 }
function Click($proc, $x, $y) {
    MoveTo $proc $x $y; Start-Sleep -Milliseconds 300
    [AssetsDrive]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100
    [AssetsDrive]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 600
}
function Drag($proc, $x0, $y0, $x1, $y1) {
    MoveTo $proc $x0 $y0; Start-Sleep -Milliseconds 300
    [AssetsDrive]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 200
    for ($i = 1; $i -le 12; $i++) { MoveTo $proc ($x0 + ($x1 - $x0) * $i / 12) ($y0 + ($y1 - $y0) * $i / 12) }
    Start-Sleep -Milliseconds 300
    [AssetsDrive]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 700
}

$proc = Start-Process -FilePath $exe -ArgumentList @("--game", "`"$dll`"", "--project", "`"$project`"") -PassThru -RedirectStandardError "$OutDir\assets_err.txt"
$null = $proc.Handle
try {
    Start-Sleep -Seconds 6
    $proc.Refresh()
    if ($proc.HasExited) { Write-Output "EXITED code=$($proc.ExitCode)"; Get-Content "$OutDir\assets_err.txt" | Select-Object -First 20; return }
    [AssetsDrive]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero); [AssetsDrive]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero)
    [AssetsDrive]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 800

    Click $proc 673 566        # the Assets tab
    Click $proc 270 651        # expand the prefabs folder
    Shot $proc "$OutDir\assets_0_tree.png"
    Drag $proc 300 685 700 260 # the coin prefab onto an empty spot of the scene
    Shot $proc "$OutDir\assets_1_dropped.png"
    [AssetsDrive]::keybd_event(0x11, 0, 0, [UIntPtr]::Zero); [AssetsDrive]::keybd_event(0x5A, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100
    [AssetsDrive]::keybd_event(0x5A, 0, 2, [UIntPtr]::Zero); [AssetsDrive]::keybd_event(0x11, 0, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 700   # Ctrl+Z
    Shot $proc "$OutDir\assets_2_undone.png"
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}
