param([string]$Exe, [string]$OutDir)

# Launches the Scene3D sample, hides the debug panels (F1), throws three crates with Space (S1_crates.png once they have landed),
# left-clicks the floor to cast a ray (S2_picked.png: the glowing marker sits where the ray hit), then closes the window normally.
# Prints the game's console log (the "Ray hit ..." line) and the exit code, which must be 0 (GL teardown regression check).
# Click coordinates are screenshot pixels (DPI-virtualised, like the other drive scripts).

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
    $w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bmp.Size)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
}

function Tap($vk) {
    [Win32]::keybd_event($vk, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80
    [Win32]::keybd_event($vk, 0, 2, [UIntPtr]::Zero)
}

New-Item -ItemType Directory -Force $OutDir | Out-Null
$proc = Start-Process -FilePath $Exe -PassThru -RedirectStandardOutput "$OutDir\out.txt" -RedirectStandardError "$OutDir\err.txt"
$null = $proc.Handle # keeps the process handle open so ExitCode is readable after the window closes
try {
    Start-Sleep -Seconds 4
    $proc.Refresh()
    if ($proc.HasExited) { Write-Output "EXITED code=$($proc.ExitCode)"; Get-Content "$OutDir\err.txt" | Select-Object -First 20; return }
    [Win32]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 800
    Start-Sleep -Milliseconds 400
    1..3 | ForEach-Object { Tap 0x20; Start-Sleep -Milliseconds 250 }   # Space: throw a crate
    Start-Sleep -Milliseconds 2500
    Shot $proc "$OutDir\S1_crates.png"

    $rect = New-Object Win32+RECT
    [Win32]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null
    [Win32]::SetCursorPos($rect.Left + 520, $rect.Top + 470) | Out-Null
    Start-Sleep -Milliseconds 300
    [Win32]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80
    [Win32]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 500
    Shot $proc "$OutDir\S2_picked.png"

    $null = $proc.CloseMainWindow()
    $proc.WaitForExit(5000) | Out-Null
    Write-Output "Exit code: $($proc.ExitCode)"
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}
Get-Content "$OutDir\out.txt" | Select-String "Game:" | ForEach-Object { $_.Line }
