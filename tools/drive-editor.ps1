param([string]$Exe, [string]$OutDir, [switch]$Save)

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

    # Wheel down three ticks zooms out; then middle-drag pans.
    Move-Mouse $rect 518 250
    [Win32]::mouse_event(0x0800, 0, 0, [System.BitConverter]::ToUInt32([System.BitConverter]::GetBytes([int]-360), 0), [UIntPtr]::Zero); Start-Sleep -Milliseconds 400
    [Win32]::mouse_event(0x0020, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100
    foreach ($step in 1..10) { Move-Mouse $rect (518 + $step * 12) 250 }
    [Win32]::mouse_event(0x0040, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 400
    Shot $proc "$OutDir\E4_zoom_pan.png"

    # With -Save, Ctrl+S writes the edited scene over the project's Assets/scenes/level.scene.json: revert it with git afterwards.
    if ($Save) {
        [Win32]::keybd_event(0xA2, 0x1D, 0, [UIntPtr]::Zero); [Win32]::keybd_event(0x53, 0x1F, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100
        [Win32]::keybd_event(0x53, 0x1F, 2, [UIntPtr]::Zero); [Win32]::keybd_event(0xA2, 0x1D, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 500
    }
}
finally {
    if (-not $proc.HasExited) { $proc.Kill() }
}
