param([string]$Exe, [string]$OutDir)

# Drives the hierarchy tree in the 3D sample: F2 turns the editor on, an entity is dragged onto another in the Hierarchy window
# (reparent), Ctrl+Z undoes it, then a child is dropped on the "drop here to unparent" strip. Saves H1..H4 screenshots and the game's
# stdout to out.txt. Coordinates are SetCursorPos pixels (DPI-virtualised, see drive-editor.ps1) of the default window, fresh layout (delete imgui.ini first):
# rows measured from a capture, assuming the sample's room scene (Floor is the first row, "Orbiting lamp" the last).

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

function Send-Chord($vk, $scan) {
    [Win32]::keybd_event(0xA2, 0x1D, 0, [UIntPtr]::Zero); [Win32]::keybd_event($vk, $scan, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100
    [Win32]::keybd_event($vk, $scan, 2, [UIntPtr]::Zero); [Win32]::keybd_event(0xA2, 0x1D, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 400
}

function Move-Mouse($rect, $x, $y) { [Win32]::SetCursorPos($rect.Left + $x, $rect.Top + $y) | Out-Null; Start-Sleep -Milliseconds 80 }

function Drag($rect, $fromX, $fromY, $toX, $toY) {
    Move-Mouse $rect $fromX $fromY
    [Win32]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 120
    foreach ($step in 1..12) {
        $x = [int]($fromX + ($toX - $fromX) * $step / 12)
        $y = [int]($fromY + ($toY - $fromY) * $step / 12)
        Move-Mouse $rect $x $y
    }
    Start-Sleep -Milliseconds 150
    [Win32]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 400
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

    [Win32]::keybd_event(0x71, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80; [Win32]::keybd_event(0x71, 0, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 500
    Shot $proc "$OutDir\H0_editor.png"

    # Orbiting lamp (last row) onto Floor (first row): becomes the floor's child, keeping its place in the world.
    Drag $rect 118 332 110 142
    Shot $proc "$OutDir\H1_reparented.png"

    Send-Chord 0x5A 0x2C
    Shot $proc "$OutDir\H2_undone.png"

    # Orbiting cube onto the unparent strip at the bottom of the window: it becomes a root.
    Drag $rect 118 319 90 461
    Shot $proc "$OutDir\H3_unparented.png"

    Send-Chord 0x5A 0x2C
    Shot $proc "$OutDir\H4_undone_again.png"
}
finally {
    if (-not $proc.HasExited) { $proc.CloseMainWindow() | Out-Null; Start-Sleep -Seconds 2 }
    if (-not $proc.HasExited) { $proc.Kill() }
    $proc.WaitForExit()
    Write-Output "exit code $($proc.ExitCode)"
    Get-Content "$OutDir\out.txt" | Select-Object -Last 14
}
