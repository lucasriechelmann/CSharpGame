param([string]$Exe, [string]$Out, [string]$Keys = "")

# Launches the exe, hides the debug UI with F1, optionally holds extra keys (virtual-key codes, comma separated, e.g. "0x44" for D)
# for a second, then screenshots just the scene. Use it to judge rendering effects (lighting, bloom) that the debug panels cover.

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

$proc = Start-Process -FilePath $Exe -PassThru
try {
    Start-Sleep -Seconds 4
    $proc.Refresh()
    if ($proc.HasExited) { Write-Output "EXITED code=$($proc.ExitCode)"; return }
    [Win32]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 500

    [Win32]::keybd_event(0x70, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80   # F1 down
    [Win32]::keybd_event(0x70, 0, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 400  # F1 up

    $codes = @()
    if ($Keys -ne "") { $codes = $Keys.Split(",") | ForEach-Object { [byte][Convert]::ToInt32($_, 16) } }
    foreach ($code in $codes) { [Win32]::keybd_event($code, 0, 0, [UIntPtr]::Zero) }
    if ($codes.Count -gt 0) { Start-Sleep -Milliseconds 1000 }

    $rect = New-Object Win32+RECT
    [Win32]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null
    $w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bmp.Size)
    $bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
    foreach ($code in $codes) { [Win32]::keybd_event($code, 0, 2, [UIntPtr]::Zero) }
    Write-Output "Captured ${w}x${h} -> $Out"
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}
