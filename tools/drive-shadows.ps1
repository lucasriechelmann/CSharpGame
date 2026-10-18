param([string]$Exe, [string]$OutDir)

# Launches the Platformer, hides the debug UI (F1), screenshots the level with light shadows on (shadow_on.png), toggles them off with F10
# and screenshots again (shadow_off.png). Compare the two: with shadows on, the tile walls block the lanterns and cast darkness behind them.
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
    Start-Sleep -Milliseconds 1200
    [Win32]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero); [Win32]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero); [Win32]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null; Start-Sleep -Milliseconds 600

    Shot $proc "$OutDir\shadow_on.png" | Out-Null
    Tap 0x79   # F10 shadows off
    Shot $proc "$OutDir\shadow_off.png" | Out-Null

    if ($proc.HasExited) { Write-Output "EXITED during the run (code $($proc.ExitCode))"; Get-Content "$OutDir\err.txt" | Select-Object -First 20; return }
    $proc.CloseMainWindow() | Out-Null
    if ($proc.WaitForExit(10000)) { Write-Output "closed normally, exit code $($proc.ExitCode)" }
    else { Write-Output "did NOT exit after close (hung)" }
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}

