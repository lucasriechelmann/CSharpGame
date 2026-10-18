param([string]$OutDir)

# Generates the Platformer sample's pixel art so the repo needs no external assets:
#   tiles.png     80x16   5 tiles of 16x16: 1 dirt, 2 grass, 3 stone, 4 background brick, 5 grass tuft
#   hero.png     128x16   8 frames: 0-1 idle, 2-5 run, 6 jump, 7 fall
#   creatures.png 256x16  8 creature types x 2 walk frames (type n uses cells 2n and 2n+1)
#   items.png    176x16   cells 0-3 spinning coin, 4 locked exit door, 5 open exit door, 6 checkpoint flag (down), 7 checkpoint flag (raised),
#                         cells 8-10 (one 48x16 image) moving platform
# Everything faces right so horizontal flips are easy to see.

Add-Type -AssemblyName System.Drawing

function Col($r, $g, $b) { [System.Drawing.Color]::FromArgb(255, $r, $g, $b) }
function Clear-Px($bmp, $x, $y) { $bmp.SetPixel($x, $y, [System.Drawing.Color]::Transparent) }
function Fill($bmp, $x, $y, $w, $h, $c) {
    for ($j = $y; $j -lt $y + $h; $j++) { for ($i = $x; $i -lt $x + $w; $i++) { $bmp.SetPixel($i, $j, $c) } }
}
function Shade($rgb, $f) { Col ([math]::Min(255, [int]($rgb[0] * $f))) ([math]::Min(255, [int]($rgb[1] * $f))) ([math]::Min(255, [int]($rgb[2] * $f))) }

# One creature in the 16x16 cell at ($ox,$oy). $bx,$by,$bw,$bh = body rectangle; $footL/$footR = vertical foot offset
# (negative raises the foot); $crown draws the hero's crown.
function Draw-Creature($bmp, $ox, $oy, $rgb, $bx, $by, $bw, $bh, $footL, $footR, $crown) {
    $body = Col $rgb[0] $rgb[1] $rgb[2]
    $dark = Shade $rgb 0.6
    $light = Shade $rgb 1.25
    $white = Col 255 255 255
    $black = Col 20 20 30
    $gold = Col 255 215 0

    Fill $bmp ($ox + $bx) ($oy + $by) $bw $bh $body
    $right = $bw - 1   # computed first: inside @( ) a comma binds tighter than "-"
    $bottom = $bh - 1
    foreach ($corner in @(@(0, 0), @($right, 0), @(0, $bottom), @($right, $bottom))) {
        Clear-Px $bmp ($ox + $bx + $corner[0]) ($oy + $by + $corner[1])
    }
    Fill $bmp ($ox + $bx + 1) ($oy + $by) ($bw - 2) 1 $light

    Fill $bmp ($ox + $bx + 1) ($oy + $by + $bh + $footL) 3 1 $dark
    Fill $bmp ($ox + $bx + $bw - 4) ($oy + $by + $bh + $footR) 3 1 $dark

    $eye1 = $bx + 2
    $eye2 = $bx + $bw - 4
    Fill $bmp ($ox + $eye1) ($oy + $by + 2) 3 3 $white
    Fill $bmp ($ox + $eye2) ($oy + $by + 2) 3 3 $white
    Fill $bmp ($ox + $eye1 + 2) ($oy + $by + 3) 1 2 $black
    Fill $bmp ($ox + $eye2 + 2) ($oy + $by + 3) 1 2 $black
    Fill $bmp ($ox + $bx + 4) ($oy + $by + $bh - 2) 3 1 $dark

    if ($crown) {
        Fill $bmp ($ox + $bx + 2) ($oy + $by - 2) 6 2 $gold
        foreach ($sx in @(2, 5, 7)) { $bmp.SetPixel($ox + $bx + $sx, $oy + $by - 3, $gold) }
    }
}

function New-Sheet($w, $h) { $b = New-Object System.Drawing.Bitmap $w, $h; $b.MakeTransparent(); return $b }

# ---- hero ----
$hero = New-Sheet 128 16
$green = @(80, 200, 90)
#                     cell bx by bw bh footL footR crown
$frames = @(
    @(0, 3, 6, 10, 8,  0,  0, $true),   # idle 0
    @(1, 3, 7, 10, 7,  0,  0, $true),   # idle 1 (squashed)
    @(2, 3, 6, 10, 8,  0, -1, $true),   # run 0
    @(3, 3, 5, 10, 8,  0,  0, $true),   # run 1 (bob up)
    @(4, 3, 6, 10, 8, -1,  0, $true),   # run 2
    @(5, 3, 5, 10, 8,  0,  0, $true),   # run 3 (bob up)
    @(6, 3, 3,  9, 11, 0,  0, $true),   # jump (stretched)
    @(7, 2, 6, 12, 8,  0,  0, $true)    # fall (wide)
)
foreach ($f in $frames) { Draw-Creature $hero ($f[0] * 16) 0 $green $f[1] $f[2] $f[3] $f[4] $f[5] $f[6] $f[7] }
$hero.Save((Join-Path $OutDir "hero.png"), [System.Drawing.Imaging.ImageFormat]::Png)

# ---- creatures ----
$creatures = New-Sheet 256 16
$palette = @(@(80, 140, 230), @(230, 80, 80), @(235, 200, 60), @(160, 90, 220), @(240, 140, 60), @(60, 200, 190), @(240, 130, 190), @(150, 150, 160))
for ($t = 0; $t -lt 8; $t++) {
    Draw-Creature $creatures (($t * 2) * 16) 0 $palette[$t] 3 6 10 8 0 -1 $false
    Draw-Creature $creatures (($t * 2 + 1) * 16) 0 $palette[$t] 3 6 10 8 -1 0 $false
}
$creatures.Save((Join-Path $OutDir "creatures.png"), [System.Drawing.Imaging.ImageFormat]::Png)

# ---- tiles ----
$tiles = New-Sheet 80 16
$rng = New-Object System.Random 7
$dirtBase = Col 121 85 58
$dirtDark = Col 96 66 44
$dirtLight = Col 140 100 70
$grass = Col 74 168 76
$grassLight = Col 110 200 100

function Draw-Dirt($bmp, $ox) {
    Fill $bmp $ox 0 16 16 $dirtBase
    for ($n = 0; $n -lt 24; $n++) {
        $c = if ($rng.Next(2) -eq 0) { $dirtDark } else { $dirtLight }
        $bmp.SetPixel($ox + $rng.Next(16), $rng.Next(16), $c)
    }
}
function Draw-Bricks($bmp, $ox, $base, $mortar, $highlight) {
    Fill $bmp $ox 0 16 16 $base
    foreach ($y in @(7, 15)) { Fill $bmp $ox $y 16 1 $mortar }
    for ($y = 0; $y -lt 7; $y++) { $bmp.SetPixel($ox + 7, $y, $mortar) }
    for ($y = 8; $y -lt 15; $y++) { $bmp.SetPixel($ox + 3, $y, $mortar); $bmp.SetPixel($ox + 11, $y, $mortar) }
    if ($highlight -ne $null) {
        foreach ($p in @(@(0, 0), @(8, 0), @(0, 8), @(4, 8), @(12, 8))) { $bmp.SetPixel($ox + $p[0], $p[1], $highlight) }
    }
}

Draw-Dirt $tiles 0                                            # 1 dirt
Draw-Dirt $tiles 16                                           # 2 grass: dirt with a green cap
Fill $tiles 16 0 16 3 $grass
Fill $tiles 16 0 16 1 $grassLight
for ($x = 0; $x -lt 16; $x++) { if ($rng.Next(2) -eq 0) { $tiles.SetPixel(16 + $x, 3, $grass) } }
Draw-Bricks $tiles 32 (Col 112 118 134) (Col 78 84 100) (Col 140 146 162)   # 3 stone
Draw-Bricks $tiles 48 (Col 40 46 68) (Col 32 37 56) $null                   # 4 background brick
foreach ($blade in @(@(3, 4), @(6, 6), @(9, 5), @(12, 3))) {                 # 5 grass tuft (transparent background)
    Fill $tiles (64 + $blade[0]) (16 - $blade[1]) 1 $blade[1] $grass
    Fill $tiles (64 + $blade[0] + 1) (16 - $blade[1] + 2) 1 ($blade[1] - 2) $grassLight
}
$tiles.Save((Join-Path $OutDir "tiles.png"), [System.Drawing.Imaging.ImageFormat]::Png)

# ---- items ----
$items = New-Sheet 176 16
$goldEdge = Col 176 120 20
$goldBody = Col 255 205 40
$goldShine = Col 255 245 170
foreach ($frame in 0..3) {
    $rx = @(5.0, 3.2, 1.2, 3.2)[$frame]   # the coin turns edge-on and back, so its width pulses
    $ox = $frame * 16
    for ($y = 0; $y -lt 16; $y++) {
        for ($x = 0; $x -lt 16; $x++) {
            $dx = ($x - 7.5) / $rx; $dy = ($y - 7.5) / 5.5
            $d = $dx * $dx + $dy * $dy
            if ($d -le 1.0) { $items.SetPixel($ox + $x, $y, $(if ($d -gt 0.62) { $goldEdge } else { $goldBody })) }
        }
    }
    if ($rx -gt 3) { $items.SetPixel($ox + 6, 5, $goldShine); $items.SetPixel($ox + 6, 6, $goldShine) }
}
# Exit door: stone frame around a dark (locked, red padlock) or glowing cyan (open) doorway.
foreach ($cell in 4..5) {
    $ox = $cell * 16
    Fill $items ($ox + 2) 1 12 15 (Col 112 118 134)
    Fill $items ($ox + 2) 1 12 1 (Col 140 146 162)
    if ($cell -eq 4) {
        Fill $items ($ox + 4) 4 8 12 (Col 24 26 40)
        Fill $items ($ox + 6) 9 4 4 (Col 200 60 60)
        Fill $items ($ox + 7) 7 2 2 (Col 200 60 60)
    } else {
        Fill $items ($ox + 4) 4 8 12 (Col 90 230 255)
        Fill $items ($ox + 6) 6 4 10 (Col 220 255 255)
    }
}
# Checkpoint flag: a pole with a dull grey-blue flag (6) that turns bright green and sits higher up the pole once reached (7).
foreach ($cell in 6..7) {
    $ox = $cell * 16
    Fill $items ($ox + 4) 1 2 15 (Col 170 170 182)
    Fill $items ($ox + 3) 14 4 2 (Col 120 120 132)
    $top = $(if ($cell -eq 6) { 7 } else { 1 })
    $flagBody = $(if ($cell -eq 6) { Col 110 120 150 } else { Col 80 220 110 })
    $flagEdge = $(if ($cell -eq 6) { Col 80 88 112 } else { Col 40 150 70 })
    foreach ($row in 0..4) {
        $width = 7 - [math]::Abs($row - 2) * 2
        Fill $items ($ox + 6) ($top + $row) $width 1 $(if ($row -eq 0 -or $row -eq 4) { $flagEdge } else { $flagBody })
    }
}
# Moving platform: a 48x16 wooden plank with a grass top, drawn at 2x so it matches the 96x32 collider.
Fill $items 128 0 48 16 (Col 150 100 60)
Fill $items 128 0 48 3 (Col 80 200 90)
Fill $items 128 0 48 1 (Col 130 230 120)
Fill $items 128 14 48 2 (Col 96 62 36)
foreach ($seam in 143, 159) { Fill $items $seam 3 1 11 (Col 110 72 42) }
$items.Save((Join-Path $OutDir "items.png"), [System.Drawing.Imaging.ImageFormat]::Png)

Write-Output "Wrote tiles.png, hero.png, creatures.png, items.png to $OutDir"
