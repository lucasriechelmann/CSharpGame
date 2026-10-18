param([string]$OutDir, [string]$Configuration = "Debug")

# Drives Edit > Add 3D in the editor on Orb Run: opens the Edit menu and the Add 3D submenu (place3d_0_menu), adds a solid box (place3d_1_added: the
# new "Platform" is selected in the hierarchy and drawn where the orbit camera looks), then undoes it with Ctrl+Z (place3d_2_undone).
# Window-relative pixels for the default 1280x720 layout.

$root = Split-Path -Parent $PSScriptRoot
Remove-Item (Join-Path $env:APPDATA "Game Editor\layout.ini") -ErrorAction SilentlyContinue
$exe = Join-Path $root "src\Engine.Editor\bin\$Configuration\net10.0\Editor.exe"
$project = Join-Path $root "samples\Arena3D\Arena3D.csgame.json"

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class PlaceDrive {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
}
"@
[PlaceDrive]::SetProcessDPIAware() | Out-Null
function WindowRect($proc) { $r = New-Object PlaceDrive+RECT; [PlaceDrive]::GetWindowRect($proc.MainWindowHandle, [ref]$r) | Out-Null; return $r }
function Shot($proc, $path) {
    $r = WindowRect $proc; $w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    [System.Drawing.Graphics]::FromImage($bmp).CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); Write-Output "shot $path"
}
function Click($proc, $x, $y) {
    $r = WindowRect $proc
    [PlaceDrive]::SetCursorPos($r.Left + $x, $r.Top + $y) | Out-Null; Start-Sleep -Milliseconds 300
    [PlaceDrive]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100
    [PlaceDrive]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 600
}
function Hover($proc, $x, $y) {
    $r = WindowRect $proc
    [PlaceDrive]::SetCursorPos($r.Left + $x, $r.Top + $y) | Out-Null; Start-Sleep -Milliseconds 700
}

$proc = Start-Process -FilePath $exe -ArgumentList @("`"$project`"", "--no-build") -PassThru
$null = $proc.Handle
try {
    Start-Sleep -Seconds 7
    [PlaceDrive]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero); [PlaceDrive]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero)
    [PlaceDrive]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 600

    Click $proc 66 52         # the Edit menu
    Hover $proc 100 157       # Add 3D
    Shot $proc "$OutDir\place3d_0_menu.png"
    Click $proc 100 157       # Add 3D (opens the submenu if hover did not)
    Click $proc 250 180       # Solid box (platform)
    Shot $proc "$OutDir\place3d_1_added.png"
    [PlaceDrive]::keybd_event(0xA2, 0x1D, 0, [UIntPtr]::Zero); [PlaceDrive]::keybd_event(0x5A, 0x2C, 0, [UIntPtr]::Zero)
    [PlaceDrive]::keybd_event(0x5A, 0x2C, 2, [UIntPtr]::Zero); [PlaceDrive]::keybd_event(0xA2, 0x1D, 2, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 700
    Shot $proc "$OutDir\place3d_2_undone.png"
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}
