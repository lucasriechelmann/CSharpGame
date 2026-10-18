param([string]$Exe, [string]$OutDir)

# Drives the 3D editor view in the 3D sample: F1 hides the debug panels, F2 opens the editor, a click selects the red cube (outline + gizmo),
# F frames it, a middle-drag orbits, Shift + middle-drag pans, the wheel zooms; then W/E/R switch the gizmo to move, rotate and scale and
# each is dragged once (Ctrl+Z undoes the move), a Ctrl+click in the Hierarchy adds the gold cube to the selection, Ctrl+D duplicates both
# and Delete removes the copies. Saves V1..V10 screenshots and the game's stdout to out.txt (the editor log lines show each step).
# Cursor coordinates are DPI-virtualised (see drive-editor.ps1) and assume the default window and a fresh layout: delete imgui.ini first.

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class Win32 {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, int data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
}
"@

function Shot($proc, $path) {
    $rect = New-Object Win32+RECT
    [Win32]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null
    $bmp = New-Object System.Drawing.Bitmap ($rect.Right - $rect.Left), ($rect.Bottom - $rect.Top)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bmp.Size)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
}

function Tap($vk) { [Win32]::keybd_event($vk, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80; [Win32]::keybd_event($vk, 0, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 400 }
function Chord($vk, $scan) {
    [Win32]::keybd_event(0xA2, 0x1D, 0, [UIntPtr]::Zero); [Win32]::keybd_event($vk, $scan, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100
    [Win32]::keybd_event($vk, $scan, 2, [UIntPtr]::Zero); [Win32]::keybd_event(0xA2, 0x1D, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 400
}
function Move-Mouse($rect, $x, $y) { [Win32]::SetCursorPos($rect.Left + $x, $rect.Top + $y) | Out-Null; Start-Sleep -Milliseconds 80 }
function Click($rect, $x, $y) {
    Move-Mouse $rect $x $y
    [Win32]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100
    [Win32]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 400
}
function Ctrl-Click($rect, $x, $y) {
    [Win32]::keybd_event(0xA2, 0x1D, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100
    Click $rect $x $y
    [Win32]::keybd_event(0xA2, 0x1D, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 300
}
function Drag($rect, $x, $y, $dx, $dy) {
    Move-Mouse $rect $x $y
    [Win32]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 150
    foreach ($step in 1..10) { Move-Mouse $rect ($x + [int]($dx * $step / 10)) ($y + [int]($dy * $step / 10)) }
    Start-Sleep -Milliseconds 150
    [Win32]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 400
}
function Middle-Drag($rect, $x, $y, $dx, $dy) {
    Move-Mouse $rect $x $y
    [Win32]::mouse_event(0x0020, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 120
    foreach ($step in 1..10) { Move-Mouse $rect ($x + [int]($dx * $step / 10)) ($y + [int]($dy * $step / 10)) }
    [Win32]::mouse_event(0x0040, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 400
}

$proc = Start-Process -FilePath $Exe -PassThru -RedirectStandardError "$OutDir\err.txt" -RedirectStandardOutput "$OutDir\out.txt"
$null = $proc.Handle
try {
    Start-Sleep -Seconds 4
    $proc.Refresh()
    if ($proc.HasExited) { Write-Output "EXITED code=$($proc.ExitCode)"; Get-Content "$OutDir\err.txt" | Select-Object -First 20; return }
    [Win32]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 800
    $rect = New-Object Win32+RECT
    [Win32]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null

    Tap 0x70   # F1: hide the debug panels
    Tap 0x71   # F2: editor on
    Shot $proc "$OutDir\V1_editor.png"

    Click $rect 351 323   # the red cube
    Shot $proc "$OutDir\V2_selected.png"

    Tap 0x46   # F: frame the selection, which puts the gizmo origin in the middle of the view (cursor 520, 317)
    Shot $proc "$OutDir\V3_framed.png"

    Drag $rect 552 317 40 0     # W (move, the default): drag the X arrow right
    Shot $proc "$OutDir\V4_moved.png"
    Chord 0x5A 0x2C             # Ctrl+Z
    Shot $proc "$OutDir\V4u_move_undone.png"

    Tap 0x45                    # E: rotate
    Drag $rect 556 281 (-72) 0  # grab the Z ring at 45 degrees up-right and sweep to 45 degrees up-left
    Shot $proc "$OutDir\V5_rotated.png"

    Tap 0x52                    # R: scale
    Drag $rect 552 317 40 0     # stretch along X
    Shot $proc "$OutDir\V6_scaled.png"

    Middle-Drag $rect 700 200 120 (-40)   # orbit
    Shot $proc "$OutDir\V7_orbited.png"

    Tap 0x57                    # W again
    Click $rect 118 183         # Hierarchy: Blue sphere (plain click: select just it)
    Ctrl-Click $rect 118 196    # Hierarchy: Gold cube (Ctrl+click adds it)
    Shot $proc "$OutDir\V8_two_selected.png"

    Chord 0x44 0x20             # Ctrl+D: duplicate both
    Shot $proc "$OutDir\V9_duplicated.png"

    [Win32]::keybd_event(0x2E, 0x53, 1, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80; [Win32]::keybd_event(0x2E, 0x53, 3, [UIntPtr]::Zero); Start-Sleep -Milliseconds 500   # Delete (an extended key): removes the two copies
    Shot $proc "$OutDir\V10_deleted.png"
}
finally {
    if (-not $proc.HasExited) { $proc.CloseMainWindow() | Out-Null; Start-Sleep -Seconds 2 }
    if (-not $proc.HasExited) { $proc.Kill() }
    $proc.WaitForExit()
    Write-Output "exit code $($proc.ExitCode)"
    Get-Content "$OutDir\out.txt" | Select-Object -Last 14
}
