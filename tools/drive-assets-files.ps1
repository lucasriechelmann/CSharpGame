param([string]$OutDir, [string]$Configuration = "Debug")

# Drives the Assets window's file features on a throwaway 2D platformer project made with `Editor --new`: expands the sprites folder and hovers the
# picture for its thumbnail (assetfiles_0_thumb.png), right-clicks it for the Rename/Delete menu (assetfiles_1_menu.png), renames it through the
# dialog (assetfiles_2_rename.png) and shows the tree afterwards (assetfiles_3_done.png). Window-relative pixels for the default 1280x720 layout.

$root = Split-Path -Parent $PSScriptRoot
Remove-Item (Join-Path $env:APPDATA "Game Editor\layout.ini") -ErrorAction SilentlyContinue
$exe = Join-Path $root "src\Engine.Editor\bin\$Configuration\net10.0\Editor.exe"
$work = Join-Path ([IO.Path]::GetTempPath()) ("assetfiles-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force $work | Out-Null
& $exe --new $work Demo --template platformer | Out-Null
$project = Join-Path $work "Demo\Demo.csgame.json"

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class FilesDrive {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
}
"@
[FilesDrive]::SetProcessDPIAware() | Out-Null
function WindowRect($proc) { $r = New-Object FilesDrive+RECT; [FilesDrive]::GetWindowRect($proc.MainWindowHandle, [ref]$r) | Out-Null; return $r }
function Shot($proc, $path) {
    $r = WindowRect $proc; $w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    [System.Drawing.Graphics]::FromImage($bmp).CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); Write-Output "shot $path"
}
function MoveTo($proc, $x, $y) { $r = WindowRect $proc; [FilesDrive]::SetCursorPos($r.Left + $x, $r.Top + $y) | Out-Null; Start-Sleep -Milliseconds 700 }
function Click($proc, $x, $y, [switch]$Right) {
    MoveTo $proc $x $y
    $down = if ($Right) { 0x0008 } else { 0x0002 }; $up = if ($Right) { 0x0010 } else { 0x0004 }
    [FilesDrive]::mouse_event($down, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100
    [FilesDrive]::mouse_event($up, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 600
}
function TypeText($text) {
    foreach ($ch in $text.ToCharArray()) {
        $vk = if ($ch -eq '.') { [byte]0xBE } else { [byte][char]::ToUpper($ch) }
        [FilesDrive]::keybd_event($vk, 0, 0, [UIntPtr]::Zero); [FilesDrive]::keybd_event($vk, 0, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 60
    }
}

$proc = Start-Process -FilePath $exe -ArgumentList @("`"$project`"", "--no-build") -PassThru
$null = $proc.Handle
try {
    Start-Sleep -Seconds 7
    [FilesDrive]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero); [FilesDrive]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero)
    [FilesDrive]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 600

    Click $proc 543 572                    # the Assets tab
    Click $proc 268 688                    # expand the sprites folder
    MoveTo $proc 300 710                   # hover square.png
    Shot $proc "$OutDir\assetfiles_0_thumb.png"
    Click $proc 300 710 -Right             # right-click it
    Shot $proc "$OutDir\assetfiles_1_menu.png"
    Click $proc 335 716                    # Rename...
    Shot $proc "$OutDir\assetfiles_2_rename.png"
    TypeText "box.png"                     # the name is selected, so this replaces it
    [FilesDrive]::keybd_event(0x0D, 0, 0, [UIntPtr]::Zero); [FilesDrive]::keybd_event(0x0D, 0, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 800
    Shot $proc "$OutDir\assetfiles_3_done.png"
    Write-Output ("renamed on disk: " + (Test-Path (Join-Path $work "Demo\Assets\sprites\box.png")))
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}
