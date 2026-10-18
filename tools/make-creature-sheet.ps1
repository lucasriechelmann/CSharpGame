param([string]$Out)

Add-Type -AssemblyName System.Drawing

# 4 columns x 2 rows of 16x16 creatures. Cell 0 (top-left) is the hero and wears a crown.
$colors = @(
    @(80, 200, 90), @(80, 140, 230), @(230, 80, 80), @(235, 200, 60),
    @(160, 90, 220), @(240, 140, 60), @(60, 200, 190), @(240, 130, 190)
)

$bmp = New-Object System.Drawing.Bitmap 64, 32
$bmp.MakeTransparent()

function Px($bmp, $x, $y, $c) { $bmp.SetPixel($x, $y, $c) }
function Rect($bmp, $x0, $y0, $w, $h, $c) {
    for ($y = $y0; $y -lt $y0 + $h; $y++) { for ($x = $x0; $x -lt $x0 + $w; $x++) { $bmp.SetPixel($x, $y, $c) } }
}

for ($i = 0; $i -lt 8; $i++) {
    $ox = ($i % 4) * 16
    $oy = [math]::Floor($i / 4) * 16
    $rgb = $colors[$i]
    $body = [System.Drawing.Color]::FromArgb(255, $rgb[0], $rgb[1], $rgb[2])
    $dark = [System.Drawing.Color]::FromArgb(255, [int]($rgb[0] * 0.6), [int]($rgb[1] * 0.6), [int]($rgb[2] * 0.6))
    $light = [System.Drawing.Color]::FromArgb(255, [math]::Min(255, [int]($rgb[0] * 1.25 + 20)), [math]::Min(255, [int]($rgb[1] * 1.25 + 20)), [math]::Min(255, [int]($rgb[2] * 1.25 + 20)))
    $white = [System.Drawing.Color]::White
    $black = [System.Drawing.Color]::FromArgb(255, 20, 20, 30)
    $gold = [System.Drawing.Color]::FromArgb(255, 255, 215, 0)

    # Body with rounded corners, feet, highlight.
    Rect $bmp ($ox + 3) ($oy + 6) 10 8 $body
    foreach ($c in @(@(3, 6), @(12, 6), @(3, 13), @(12, 13))) { $bmp.SetPixel($ox + $c[0], $oy + $c[1], [System.Drawing.Color]::Transparent) }
    Rect $bmp ($ox + 4) ($oy + 6) 8 1 $light
    Rect $bmp ($ox + 4) ($oy + 14) 3 1 $dark
    Rect $bmp ($ox + 9) ($oy + 14) 3 1 $dark

    # Eyes look right so horizontal flips are easy to see.
    Rect $bmp ($ox + 5) ($oy + 8) 3 3 $white
    Rect $bmp ($ox + 9) ($oy + 8) 3 3 $white
    Rect $bmp ($ox + 7) ($oy + 9) 1 2 $black
    Rect $bmp ($ox + 11) ($oy + 9) 1 2 $black

    # Variation: a different mouth per creature.
    if ($i % 2 -eq 0) { Rect $bmp ($ox + 7) ($oy + 12) 3 1 $dark } else { Px $bmp ($ox + 9) ($oy + 12) $dark }

    # Hero crown.
    if ($i -eq 0) {
        Rect $bmp ($ox + 5) ($oy + 4) 6 2 $gold
        Px $bmp ($ox + 5) ($oy + 3) $gold
        Px $bmp ($ox + 8) ($oy + 3) $gold
        Px $bmp ($ox + 10) ($oy + 3) $gold
    }
}

$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output "Wrote $Out"
