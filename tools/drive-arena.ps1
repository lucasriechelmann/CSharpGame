param([string]$Exe, [string]$OutDir)

# Plays a few seconds of Orb Run (the Arena3D sample): screenshots the start (arena_0_start.png), walks left along the bottom edge and a little
# back to collect the orb at (-9, 9) (arena_1_orb.png: the HUD says Orbs 1/12), jumps onto the first platform (arena_2_platform.png) and walks
# off the arena edge to fall (arena_3_fell.png: the clock lost 5 seconds and the player is back at the start). Keys are held for fixed times,
# so the exact positions can vary a little; look at the screenshots.

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class ArenaDrive {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
}
"@
[ArenaDrive]::SetProcessDPIAware() | Out-Null
function Shot($proc, $path) {
    $r = New-Object ArenaDrive+RECT; [ArenaDrive]::GetWindowRect($proc.MainWindowHandle, [ref]$r) | Out-Null
    $w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    [System.Drawing.Graphics]::FromImage($bmp).CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); Write-Output "shot $path"
}
function Hold($vk, $ms) {
    [ArenaDrive]::keybd_event($vk, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds $ms
    [ArenaDrive]::keybd_event($vk, 0, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 150
}

$proc = Start-Process -FilePath $Exe -PassThru -RedirectStandardError "$OutDir\arena_err.txt"
try {
    Start-Sleep -Seconds 4
    $proc.Refresh()
    if ($proc.HasExited) { Write-Output "EXITED code=$($proc.ExitCode)"; Get-Content "$OutDir\arena_err.txt" | Select-Object -First 20; return }
    [ArenaDrive]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero); [ArenaDrive]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero)
    [ArenaDrive]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 600

    Shot $proc "$OutDir\arena_0_start.png"
    Hold 0x41 1800                         # A: walk left
    Hold 0x53 280                          # S: a little back, to pass through the orb at (-9, 9)
    Start-Sleep -Milliseconds 300
    Shot $proc "$OutDir\arena_1_orb.png"
    Hold 0x57 600                          # W: forward, toward the first platform
    [ArenaDrive]::keybd_event(0x20, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100; [ArenaDrive]::keybd_event(0x20, 0, 2, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 900
    Shot $proc "$OutDir\arena_2_platform.png"
    Hold 0x44 4500                         # D: all the way across and off the right edge
    Start-Sleep -Milliseconds 700
    Shot $proc "$OutDir\arena_3_fell.png"
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}
