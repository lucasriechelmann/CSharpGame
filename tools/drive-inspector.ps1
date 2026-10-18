param([string]$Exe, [string]$OutDir)

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
}
"@

function Shot($proc, $path) {
    $rect = New-Object Win32+RECT
    [Win32]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null
    $w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bmp.Size)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
}

function Click($rect, $x, $y) {
    [Win32]::SetCursorPos($rect.Left + $x, $rect.Top + $y) | Out-Null
    Start-Sleep -Milliseconds 200
    [Win32]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80
    [Win32]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 300
}

function Drag($rect, $x0, $y0, $x1, $y1) {
    [Win32]::SetCursorPos($rect.Left + $x0, $rect.Top + $y0) | Out-Null
    Start-Sleep -Milliseconds 200
    [Win32]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    for ($i = 1; $i -le 10; $i++) {
        $x = $x0 + ($x1 - $x0) * $i / 10
        $y = $y0 + ($y1 - $y0) * $i / 10
        [Win32]::SetCursorPos($rect.Left + [int]$x, $rect.Top + [int]$y) | Out-Null
        Start-Sleep -Milliseconds 50
    }
    [Win32]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 300
}

$proc = Start-Process -FilePath $Exe -PassThru -RedirectStandardError "$OutDir\err.txt"
$null = $proc.Handle # keeps the process handle open so ExitCode is readable after the window closes
try {
    Start-Sleep -Seconds 4
    $proc.Refresh()
    if ($proc.HasExited) { Write-Output "EXITED code=$($proc.ExitCode)"; Get-Content "$OutDir\err.txt" | Select-Object -First 20; return }
    [Win32]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 800

    $rect = New-Object Win32+RECT
    [Win32]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null

    # Pull the Inspector window (title bar at about 700,430 on a fresh layout) up so its details pane is on screen.
    Drag $rect 700 430 330 60
    Shot $proc "$OutDir\I1_moved.png"

    # Select the hero in the entity list (the "Hero" row sits at about 215,147 after the drag).
    Click $rect 215 147
    Shot $proc "$OutDir\I2_selected.png"

    $proc.CloseMainWindow() | Out-Null
    if ($proc.WaitForExit(10000)) { Write-Output "closed normally, exit code $($proc.ExitCode)" }
    else { Write-Output "did NOT exit after close (hung)" }
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}
