param([string]$OutDir, [string]$Project, [switch]$Reload, [string]$Configuration = "Debug")

# Opens a project in the editor and drives the Project menu: shows the menu (menu_0), runs Build (menu_1: the Log tab shows the build output),
# runs Validate assets (menu_2) and finally Build and reload game code (menu_3: a second editor window appears and the first closes).
# Positions are window-relative pixels for the default 1280x720 layout, with the menu entries at the offsets of the Project menu.

$root = Split-Path -Parent $PSScriptRoot
Remove-Item (Join-Path $env:APPDATA "Game Editor\layout.ini") -ErrorAction SilentlyContinue
$exe = Join-Path $root "src\Engine.Editor\bin\$Configuration\net10.0\Editor.exe"
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class MenuDrive {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
}
"@
[MenuDrive]::SetProcessDPIAware() | Out-Null
function WindowRect($proc) { $r = New-Object MenuDrive+RECT; [MenuDrive]::GetWindowRect($proc.MainWindowHandle, [ref]$r) | Out-Null; return $r }
function Shot($proc, $path) {
    $r = WindowRect $proc; $w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    [System.Drawing.Graphics]::FromImage($bmp).CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); Write-Output "shot $path"
}
function Click($proc, $x, $y) {
    $r = WindowRect $proc
    [MenuDrive]::SetCursorPos($r.Left + $x, $r.Top + $y) | Out-Null; Start-Sleep -Milliseconds 300
    [MenuDrive]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100
    [MenuDrive]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 600
}

$proc = Start-Process -FilePath $exe -ArgumentList @("`"$Project`"", "--no-build") -PassThru
$null = $proc.Handle
try {
    Start-Sleep -Seconds 6
    [MenuDrive]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero); [MenuDrive]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero)
    [MenuDrive]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 600

    Click $proc 100 52                      # the Project menu
    Shot $proc "$OutDir\menu_0_open.png"
    Click $proc 100 80                      # Build
    Click $proc 489 572                     # the Log tab
    Start-Sleep -Seconds 5
    Shot $proc "$OutDir\menu_1_build.png"
    Click $proc 100 52
    Click $proc 100 130                     # Validate assets
    Shot $proc "$OutDir\menu_2_validate.png"
    if ($Reload) {
        $before = $proc.Id
        Click $proc 100 52
        Click $proc 100 106                 # Build and reload game code
        $deadline = (Get-Date).AddSeconds(60); $next = $null
        while ((Get-Date) -lt $deadline -and -not $next) {
            Start-Sleep -Seconds 2
            $next = Get-Process -Name Editor -ErrorAction SilentlyContinue | Where-Object { $_.Id -ne $before -and $_.MainWindowTitle -like "*Game Editor" } | Select-Object -First 1
        }
        if ($next) {
            Start-Sleep -Seconds 4
            Shot $next "$OutDir\menu_3_reloaded.png"
            Write-Output "Reloaded: new editor process $($next.Id); the old one exited: $($proc.HasExited)"
            Stop-Process -Id $next.Id -Force
        }
        else { Write-Output "No new editor appeared." }
    }
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}
