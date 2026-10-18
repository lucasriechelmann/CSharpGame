param([string]$Exe, [string]$OutDir, [switch]$Save, [switch]$Play, [switch]$Paint)

# NOTE: the click coordinates below were measured on a 125% scaled display; on other scales adjust them (the DBG line style:
# temporarily log the ImGui mouse position to calibrate).
# Drives the scene editor in the Platformer sample: F2 on, click the hero (selects it), drag its X arrow right, wheel-zoom out,
# middle-drag pan. Saves E1..E4 screenshots. Click coordinates assume the default 1280x720 window with the hero at the centre
# and a fresh layout (delete imgui.ini first). Does not save the scene.

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class Win32 {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
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

# Ctrl + key chord with real scan codes (the Silk/ImGui input needs them to see Ctrl as a modifier).
function Send-Chord($vk, $scan) {
    [Win32]::keybd_event(0xA2, 0x1D, 0, [UIntPtr]::Zero); [Win32]::keybd_event($vk, $scan, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100
    [Win32]::keybd_event($vk, $scan, 2, [UIntPtr]::Zero); [Win32]::keybd_event(0xA2, 0x1D, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 400
}

function Move-Mouse($rect, $x, $y) { [Win32]::SetCursorPos($rect.Left + $x, $rect.Top + $y) | Out-Null; Start-Sleep -Milliseconds 120 }

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

    # F1 hides the debug panels, F2 turns the editor on.
    foreach ($key in 0x70, 0x71) { [Win32]::keybd_event($key, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80; [Win32]::keybd_event($key, 0, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 300 }
    Shot $proc "$OutDir\E1_editor.png"

    # Click the hero to select it. SetCursorPos takes DPI-virtualised pixels: on a 125% display the window centre (640, 360) is at about (518, 318) here.
    Move-Mouse $rect 518 318
    [Win32]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80
    [Win32]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 400
    Shot $proc "$OutDir\E2_selected.png"

    # Drag the X arrow (40 px right of the hero) 120 px to the right.
    Move-Mouse $rect 550 318
    [Win32]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100
    foreach ($step in 1..12) { Move-Mouse $rect (550 + $step * 8) 318 }
    [Win32]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 400
    Shot $proc "$OutDir\E3_dragged.png"

    # Ctrl+Z puts the hero back, Ctrl+Y moves it again.
    Send-Chord 0x5A 0x2C
    Shot $proc "$OutDir\E3u_undone.png"
    Send-Chord 0x59 0x15
    Shot $proc "$OutDir\E3r_redone.png"

    # With -Play: F3 plays, holding D runs the hero right for a second, Ctrl+P stops and the world must be back as edited.
    if ($Play) {
        [Win32]::keybd_event(0x72, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80; [Win32]::keybd_event(0x72, 0, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 300
        [Win32]::keybd_event(0x44, 0x20, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 1000
        Shot $proc "$OutDir\P1_playing.png"
        [Win32]::keybd_event(0x44, 0x20, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 200
        [Win32]::keybd_event(0x72, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80; [Win32]::keybd_event(0x72, 0, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 400
        Shot $proc "$OutDir\P2_stopped.png"
    }

    # With -Paint: select the second tile layer in the hierarchy, press B (paint mode), drag a horizontal stroke through the empty air
    # (Q1_painted), undo it (Q2_undone) and redo it (Q3_redone).
    if ($Paint) {
        Move-Mouse $rect 41 155
        [Win32]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80
        [Win32]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 300
        [Win32]::keybd_event(0x42, 0x30, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80; [Win32]::keybd_event(0x42, 0x30, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 300
        Move-Mouse $rect 406 190
        [Win32]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100
        foreach ($step in 1..16) { Move-Mouse $rect (406 + $step * 10) 190 }
        [Win32]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 400
        Shot $proc "$OutDir\Q1_painted.png"
        Send-Chord 0x5A 0x2C
        Shot $proc "$OutDir\Q2_undone.png"
        Send-Chord 0x59 0x15
        Shot $proc "$OutDir\Q3_redone.png"
    }

    # Wheel down three ticks zooms out; then middle-drag pans.
    Move-Mouse $rect 518 250
    [Win32]::mouse_event(0x0800, 0, 0, [System.BitConverter]::ToUInt32([System.BitConverter]::GetBytes([int]-360), 0), [UIntPtr]::Zero); Start-Sleep -Milliseconds 400
    [Win32]::mouse_event(0x0020, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100
    foreach ($step in 1..10) { Move-Mouse $rect (518 + $step * 12) 250 }
    [Win32]::mouse_event(0x0040, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 400
    Shot $proc "$OutDir\E4_zoom_pan.png"

    # With -Save, Ctrl+S writes the edited scene over the project's Assets/scenes/level.scene.json: revert it with git afterwards.
    if ($Save) {
        Send-Chord 0x53 0x1F
    }
}
finally {
    if (-not $proc.HasExited) { $proc.Kill() }
}
