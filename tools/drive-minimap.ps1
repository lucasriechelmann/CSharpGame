param([string]$Exe, [string]$OutDir)

# Starts the Platformer, screenshots the radar view in the corner (minimap_0_on.png: a washed-out sepia picture of the level around the hero,
# drawn by a RenderView with its own effects), presses M to hide it (minimap_1_off.png) and again to bring it back after a display-mode
# change (F6 pixel-perfect, minimap_2_pixel.png), to check the view survives the main picture's setup.

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class MiniDrive {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
}
"@
[MiniDrive]::SetProcessDPIAware() | Out-Null
function Shot($proc, $path) {
    $r = New-Object MiniDrive+RECT; [MiniDrive]::GetWindowRect($proc.MainWindowHandle, [ref]$r) | Out-Null
    $w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    [System.Drawing.Graphics]::FromImage($bmp).CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); Write-Output "shot $path"
}
function Tap($vk) { [MiniDrive]::keybd_event($vk, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80; [MiniDrive]::keybd_event($vk, 0, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 500 }

$proc = Start-Process -FilePath $Exe -PassThru -RedirectStandardError "$OutDir\minimap_err.txt"
try {
    Start-Sleep -Seconds 4
    $proc.Refresh()
    if ($proc.HasExited) { Write-Output "EXITED code=$($proc.ExitCode)"; Get-Content "$OutDir\minimap_err.txt" | Select-Object -First 20; return }
    [MiniDrive]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero); [MiniDrive]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero)
    [MiniDrive]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 600

    Shot $proc "$OutDir\minimap_0_on.png"
    Tap 0x4D                                # M: hide
    Shot $proc "$OutDir\minimap_1_off.png"
    Tap 0x4D                                # M: show again
    Tap 0x75                                # F6: pixel-perfect main picture
    Start-Sleep -Milliseconds 500
    Shot $proc "$OutDir\minimap_2_pixel.png"
    $proc.Refresh()
    if ($proc.HasExited) { Write-Output "EXITED code=$($proc.ExitCode)" }
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}
