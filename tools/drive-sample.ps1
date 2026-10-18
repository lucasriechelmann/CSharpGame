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

    # A: baseline
    Shot $proc "$OutDir\A_baseline.png"

    # B: click in the level to spawn creatures
    [Win32]::SetCursorPos($rect.Left + 760, $rect.Top + 300) | Out-Null
    Start-Sleep -Milliseconds 300
    [Win32]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80
    [Win32]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 500
    Shot $proc "$OutDir\B_clicked.png"

    # C: hold D (run right) for 1s, capture while held
    [Win32]::keybd_event(0x44, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 1000
    Shot $proc "$OutDir\C_holdD.png"

    # D: while still running, press Space and capture mid-jump
    [Win32]::keybd_event(0x20, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 250
    Shot $proc "$OutDir\D_jumping.png"
    [Win32]::keybd_event(0x20, 0, 2, [UIntPtr]::Zero)
    [Win32]::keybd_event(0x44, 0, 2, [UIntPtr]::Zero)

    # E: let the hero land and stop, then F5 (quick-save)
    Start-Sleep -Milliseconds 1500
    [Win32]::keybd_event(0x74, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80
    [Win32]::keybd_event(0x74, 0, 2, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 300
    Shot $proc "$OutDir\E_saved.png"

    # F: run left for 1s so the hero is somewhere else
    [Win32]::keybd_event(0x41, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 1000
    [Win32]::keybd_event(0x41, 0, 2, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 500
    Shot $proc "$OutDir\F_moved.png"

    # G: F9 (quick-load) should put the hero back where it was saved
    [Win32]::keybd_event(0x78, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80
    [Win32]::keybd_event(0x78, 0, 2, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 300
    Shot $proc "$OutDir\G_loaded.png"

    # Close the window the normal way: a clean shutdown must exit with code 0.
    $proc.CloseMainWindow() | Out-Null
    if ($proc.WaitForExit(10000)) { Write-Output "closed normally, exit code $($proc.ExitCode)" }
    else { Write-Output "did NOT exit after close (hung)" }
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}
