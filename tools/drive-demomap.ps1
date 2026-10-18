param([string]$Exe, [string]$OutDir)

# Launches the Platformer, presses F11 (imports Assets/maps/demo.tmj below the level and looks at it) and screenshots the map twice, 0.6 s
# apart (demomap_a.png, demomap_b.png): the animated tile on the upper row changes between the two, flipped tiles show on the row beneath,
# and the faded backdrop layer scrolls with parallax when the camera moves.

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class MapDrive {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
}
"@
[MapDrive]::SetProcessDPIAware() | Out-Null
function Shot($proc, $path) {
    $r = New-Object MapDrive+RECT; [MapDrive]::GetWindowRect($proc.MainWindowHandle, [ref]$r) | Out-Null
    $w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    [System.Drawing.Graphics]::FromImage($bmp).CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); Write-Output "shot $path"
}

$proc = Start-Process -FilePath $Exe -PassThru -RedirectStandardError "$OutDir\demomap_err.txt"
try {
    Start-Sleep -Seconds 4
    $proc.Refresh()
    if ($proc.HasExited) { Write-Output "EXITED code=$($proc.ExitCode)"; Get-Content "$OutDir\demomap_err.txt" | Select-Object -First 20; return }
    [MapDrive]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero); [MapDrive]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero)
    [MapDrive]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 600
    [MapDrive]::keybd_event(0x7A, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80
    [MapDrive]::keybd_event(0x7A, 0, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 700
    Shot $proc "$OutDir\demomap_a.png"
    Start-Sleep -Milliseconds 600
    Shot $proc "$OutDir\demomap_b.png"
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}
