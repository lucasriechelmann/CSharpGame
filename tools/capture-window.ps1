param([string]$Exe, [string]$Out, [int]$WaitSeconds = 4)

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class Win32 {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
}
"@

$proc = Start-Process -FilePath $Exe -PassThru -RedirectStandardError "$Out.err.txt"
try {
    Start-Sleep -Seconds $WaitSeconds
    $proc.Refresh()
    if ($proc.HasExited) { Write-Output "EXITED code=$($proc.ExitCode)"; Get-Content "$Out.err.txt" | Select-Object -First 20; return }
    [Win32]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 500
    $rect = New-Object Win32+RECT
    [Win32]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null
    $w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bmp.Size)
    $bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output "Captured ${w}x${h} -> $Out"
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}
