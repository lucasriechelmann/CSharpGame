param([string]$OutDir, [string]$Configuration = "Debug")

# Drives Save as prefab and Revert to prefab in the editor on the Platformer: selects a coin, opens the Edit menu (prefab_0_menu), chooses
# Save as prefab... (prefab_1_popup, the default name is a free one) and saves it (prefab_2_saved), drags the coin 40 pixels with the gizmo
# (prefab_3_moved) and chooses Revert to prefab (prefab_4_reverted: the coin is back). The prefab file it writes is deleted afterwards.
# Window-relative pixels for the default 1280x720 layout.

$root = Split-Path -Parent $PSScriptRoot
Remove-Item (Join-Path $env:APPDATA "Game Editor\layout.ini") -ErrorAction SilentlyContinue
$exe = Join-Path $root "src\Engine.Editor\bin\$Configuration\net10.0\Editor.exe"
$project = Join-Path $root "samples\Platformer\Platformer.csgame.json"
$prefabFile = Join-Path $root "samples\Platformer\Assets\prefabs\coin_2.prefab.json"
Remove-Item $prefabFile -ErrorAction SilentlyContinue

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class PrefabDrive {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
}
"@
[PrefabDrive]::SetProcessDPIAware() | Out-Null
function WindowRect($proc) { $r = New-Object PrefabDrive+RECT; [PrefabDrive]::GetWindowRect($proc.MainWindowHandle, [ref]$r) | Out-Null; return $r }
function Shot($proc, $path) {
    $r = WindowRect $proc; $w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    [System.Drawing.Graphics]::FromImage($bmp).CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); Write-Output "shot $path"
}
function Click($proc, $x, $y) {
    $r = WindowRect $proc
    [PrefabDrive]::SetCursorPos($r.Left + $x, $r.Top + $y) | Out-Null; Start-Sleep -Milliseconds 300
    [PrefabDrive]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100
    [PrefabDrive]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 600
}

$proc = Start-Process -FilePath $exe -ArgumentList @("`"$project`"", "--no-build") -PassThru
$null = $proc.Handle
try {
    Start-Sleep -Seconds 6
    [PrefabDrive]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero); [PrefabDrive]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero)
    [PrefabDrive]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 600

    Click $proc 872 165       # a coin in the scene
    Click $proc 66 52         # the Edit menu
    Shot $proc "$OutDir\prefab_0_menu.png"
    Click $proc 100 207       # Save as prefab...
    Shot $proc "$OutDir\prefab_1_popup.png"
    Click $proc 543 441       # the popup's Save button
    Shot $proc "$OutDir\prefab_2_saved.png"
    Write-Output ("prefab file written: " + (Test-Path $prefabFile))

    # Drag the coin 40 pixels to the right with the gizmo's free-move handle, then revert it to the prefab.
    $r = WindowRect $proc
    [PrefabDrive]::SetCursorPos($r.Left + 870, $r.Top + 166) | Out-Null; Start-Sleep -Milliseconds 400; [PrefabDrive]::SetCursorPos($r.Left + 872, $r.Top + 165) | Out-Null; Start-Sleep -Milliseconds 700
    [PrefabDrive]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 200
    for ($i = 1; $i -le 12; $i++) { [PrefabDrive]::SetCursorPos($r.Left + 872 + 4 * $i, $r.Top + 165) | Out-Null; Start-Sleep -Milliseconds 120 }
    [PrefabDrive]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 600
    Shot $proc "$OutDir\prefab_3_moved.png"
    Click $proc 66 52
    Click $proc 100 229       # Revert to prefab
    Shot $proc "$OutDir\prefab_4_reverted.png"
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
    Remove-Item $prefabFile -ErrorAction SilentlyContinue
}
