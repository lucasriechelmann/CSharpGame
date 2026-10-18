param([string]$OutDir, [string]$Location = "", [string]$Name = "HubGame", [string]$Configuration = "Debug")

# Drives the project hub end to end: opens the New project page (hub_0_home / hub_1_new), types a name and a location, presses Create, waits
# for the build and for the editor to start on the new project (hub_2_building / hub_3_editor). Positions are window-relative pixels for the
# hub's default 1000x640 window. Needs a free -Location folder (defaults to a temp folder).

$root = Split-Path -Parent $PSScriptRoot
Remove-Item (Join-Path $env:APPDATA "Game Editor\layout.ini") -ErrorAction SilentlyContinue # a fresh run builds the default window layout
$exe = Join-Path $root "src\Engine.Editor\bin\$Configuration\net10.0\Editor.exe"
if ($Location -eq "") { $Location = Join-Path $env:TEMP "hubprojects" }
if (Test-Path (Join-Path $Location $Name)) { cmd /c rmdir /s /q (Join-Path $Location $Name) }

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class HubDrive {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
}
"@
[HubDrive]::SetProcessDPIAware() | Out-Null

function WindowRect($proc) { $r = New-Object HubDrive+RECT; [HubDrive]::GetWindowRect($proc.MainWindowHandle, [ref]$r) | Out-Null; return $r }
function Shot($proc, $path) {
    $r = WindowRect $proc; $w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    [System.Drawing.Graphics]::FromImage($bmp).CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output "shot $path"
}
function Click($proc, $x, $y) {
    $r = WindowRect $proc
    [HubDrive]::SetCursorPos($r.Left + $x, $r.Top + $y) | Out-Null; Start-Sleep -Milliseconds 300
    [HubDrive]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 100
    [HubDrive]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 500
}
function TypeInto($proc, $x, $y, $text) {
    Click $proc $x $y
    [HubDrive]::keybd_event(0x11, 0, 0, [UIntPtr]::Zero); [HubDrive]::keybd_event(0x41, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80
    [HubDrive]::keybd_event(0x41, 0, 2, [UIntPtr]::Zero); [HubDrive]::keybd_event(0x11, 0, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 200   # Ctrl+A
    [System.Windows.Forms.SendKeys]::SendWait($text.Replace("{", "{{}").Replace("}", "{}}").Replace("+", "{+}").Replace("^", "{^}").Replace("%", "{%}").Replace("~", "{~}").Replace("(", "{(}").Replace(")", "{)}")); Start-Sleep -Milliseconds 300
}

$hub = Start-Process -FilePath $exe -PassThru
$null = $hub.Handle
try {
    Start-Sleep -Seconds 4
    [HubDrive]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero); [HubDrive]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero)
    [HubDrive]::SetForegroundWindow($hub.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 600

    Shot $hub "$OutDir\hub_0_home.png"
    Click $hub 110 160                      # New project
    TypeInto $hub 200 213 $Name             # name field
    Click $hub 100 296                      # the "2D platformer" template
    TypeInto $hub 300 378 $Location         # location field
    Shot $hub "$OutDir\hub_1_new.png"
    Click $hub 84 461                       # Create
    Start-Sleep -Milliseconds 800
    Shot $hub "$OutDir\hub_2_building.png"

    # The hub builds, starts the editor on the new project and closes itself.
    $deadline = (Get-Date).AddSeconds(90)
    $editor = $null
    while ((Get-Date) -lt $deadline -and -not $editor) {
        Start-Sleep -Seconds 2
        $editor = Get-Process -Name Editor -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -like "$Name*" } | Select-Object -First 1
    }
    if (-not $editor) { Write-Output "No editor window for $Name appeared."; return }
    Start-Sleep -Seconds 4
    Shot $editor "$OutDir\hub_3_editor.png"
    Write-Output "Editor opened: $($editor.MainWindowTitle)"
    Stop-Process -Id $editor.Id -Force
}
finally {
    if (-not $hub.HasExited) { Stop-Process -Id $hub.Id -Force }
}
