param([string]$Exe, [string]$OutDir)

# Drives the Platformer through its display modes and screenshots each: baseline, pixel-perfect (F6), + HDR with tone mapping (F7),
# + 4x multisampling (F8 twice), then each switched off again. Reports if the game exits early (a GL error throws) and the exit code
# of a normal close. Screenshots land in $OutDir; look at them to judge the result.

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

function Tap($vk) {
    [Win32]::keybd_event($vk, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80
    [Win32]::keybd_event($vk, 0, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 600
}

function Shot($proc, $path) {
    if ($proc.HasExited) { Write-Output "EXITED before $path (code $($proc.ExitCode))"; return $false }
    $rect = New-Object Win32+RECT
    [Win32]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null
    $w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bmp.Size)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output "shot $path (${w}x${h})"
    return $true
}

$proc = Start-Process -FilePath $Exe -PassThru -RedirectStandardError "$OutDir\err.txt"
$null = $proc.Handle
try {
    Start-Sleep -Seconds 4
    $proc.Refresh()
    if ($proc.HasExited) { Write-Output "EXITED code=$($proc.ExitCode)"; Get-Content "$OutDir\err.txt" | Select-Object -First 20; return }
    [Win32]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 500
    Tap 0x70   # F1: hide the debug UI

    Shot $proc "$OutDir\disp_0_baseline.png" | Out-Null
    Tap 0x75   # F6 pixel-perfect on
    Shot $proc "$OutDir\disp_1_pixelperfect.png" | Out-Null
    Tap 0x76   # F7 HDR on
    Shot $proc "$OutDir\disp_2_pp_hdr.png" | Out-Null
    Tap 0x77; Tap 0x77   # F8 twice: 2x then 4x
    Shot $proc "$OutDir\disp_3_pp_hdr_msaa4.png" | Out-Null
    Tap 0x75   # F6 pixel-perfect off (HDR + MSAA stay)
    Shot $proc "$OutDir\disp_4_hdr_msaa4.png" | Out-Null
    Tap 0x76   # F7 HDR off
    Tap 0x77; Tap 0x77; Tap 0x77   # F8: 8x, off, 2x ... cycle through every step
    Shot $proc "$OutDir\disp_5_msaa2.png" | Out-Null

    if ($proc.HasExited) { Write-Output "EXITED during the run (code $($proc.ExitCode))"; Get-Content "$OutDir\err.txt" | Select-Object -First 20; return }
    $proc.CloseMainWindow() | Out-Null
    if ($proc.WaitForExit(10000)) { Write-Output "closed normally, exit code $($proc.ExitCode)" }
    else { Write-Output "did NOT exit after close (hung)" }
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}
