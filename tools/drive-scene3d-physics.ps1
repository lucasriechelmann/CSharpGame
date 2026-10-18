param([string]$Exe, [string]$OutDir)

# Drives the 3D sample's physics: F1 hides the panels, the player capsule walks with the arrow keys (held for a moment each, so it
# climbs towards the ramp, walks into crates thrown with Space and jumps with Enter). Saves K0..K5 screenshots and the game's stdout to
# out.txt. Arrow keys are extended keys: they need the extended flag and the real scan codes to reach the game.

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class Win32 {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
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

function Tap($vk, $scan = 0, $extended = $false) {
    $down = if ($extended) { 1 } else { 0 }
    $up = if ($extended) { 3 } else { 2 }
    [Win32]::keybd_event($vk, $scan, $down, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80; [Win32]::keybd_event($vk, $scan, $up, [UIntPtr]::Zero); Start-Sleep -Milliseconds 300
}

function Hold($vk, $scan, $seconds) {
    [Win32]::keybd_event($vk, $scan, 1, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds ([int]($seconds * 1000))
    [Win32]::keybd_event($vk, $scan, 3, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 300
}

$proc = Start-Process -FilePath $Exe -PassThru -RedirectStandardError "$OutDir\err.txt" -RedirectStandardOutput "$OutDir\out.txt"
$null = $proc.Handle
try {
    Start-Sleep -Seconds 4
    $proc.Refresh()
    if ($proc.HasExited) { Write-Output "EXITED code=$($proc.ExitCode)"; Get-Content "$OutDir\err.txt" | Select-Object -First 20; return }
    [Win32]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 800

    Tap 0x70           # F1: hide the debug panels
    Shot $proc "$OutDir\K0_start.png"

    Hold 0x26 0x48 1.0   # Up arrow: walk away from the camera
    Shot $proc "$OutDir\K1_walked.png"

    foreach ($i in 1..3) { Tap 0x20 0x39 }   # Space: throw three crates
    Start-Sleep -Seconds 2
    Shot $proc "$OutDir\K2_crates.png"

    Hold 0x25 0x4B 1.5   # Left arrow: walk towards the ramp
    Tap 0x0D 0x1C        # Enter: jump
    Start-Sleep -Milliseconds 300
    Shot $proc "$OutDir\K3_jump.png"

    Hold 0x26 0x48 1.5   # Up arrow again
    Shot $proc "$OutDir\K4_after.png"
}
finally {
    if (-not $proc.HasExited) { $proc.CloseMainWindow() | Out-Null; Start-Sleep -Seconds 2 }
    if (-not $proc.HasExited) { $proc.Kill() }
    $proc.WaitForExit()
    Write-Output "exit code $($proc.ExitCode)"
    Get-Content "$OutDir\out.txt" | Select-Object -Last 8
}
