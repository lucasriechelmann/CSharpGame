param([string]$OutDir = "$PSScriptRoot\..\samples\Arena3D\Assets\sounds")

# Synthesises Orb Run's sound effects as 16-bit mono 22.05 kHz WAV files, so the repo needs no external audio.
#   orb.wav  - bright two-note pickup chime
#   jump.wav - quick rising sine chirp
#   fall.wav - long falling tone
#   win.wav  - rising arpeggio
#   lose.wav - slow descending tone

$rate = 22050
New-Item -ItemType Directory -Force $OutDir | Out-Null

function Write-Wav([string]$path, [double[]]$samples) {
    $stream = New-Object System.IO.MemoryStream
    $w = New-Object System.IO.BinaryWriter($stream)
    $dataBytes = $samples.Length * 2
    $w.Write([Text.Encoding]::ASCII.GetBytes("RIFF")); $w.Write([int](36 + $dataBytes)); $w.Write([Text.Encoding]::ASCII.GetBytes("WAVE"))
    $w.Write([Text.Encoding]::ASCII.GetBytes("fmt ")); $w.Write([int]16); $w.Write([int16]1); $w.Write([int16]1)
    $w.Write([int]$rate); $w.Write([int]($rate * 2)); $w.Write([int16]2); $w.Write([int16]16)
    $w.Write([Text.Encoding]::ASCII.GetBytes("data")); $w.Write([int]$dataBytes)
    foreach ($s in $samples) {
        $clamped = [Math]::Max(-1.0, [Math]::Min(1.0, $s))
        $w.Write([int16][Math]::Round($clamped * 32000))
    }
    $w.Flush()
    [IO.File]::WriteAllBytes($path, $stream.ToArray())
    Write-Output "wrote $path ($($samples.Length) samples)"
}

# A sine note with a short attack and an exponential decay, added into $buffer starting at $start.
function Add-Note([double[]]$buffer, [int]$start, [double]$freq, [double]$seconds, [double]$gain) {
    $n = [int]($rate * $seconds)
    for ($i = 0; $i -lt $n -and ($start + $i) -lt $buffer.Length; $i++) {
        $attack = [Math]::Min(1.0, $i / ($rate * 0.005))
        $decay = [Math]::Exp(-4.0 * $i / $n)
        $buffer[$start + $i] += [Math]::Sin(2 * [Math]::PI * $freq * $i / $rate) * $gain * $attack * $decay
    }
}

$orb = New-Object double[] ([int]($rate * 0.4))
Add-Note $orb 0 880 0.25 0.45
Add-Note $orb ([int]($rate * 0.08)) 1320 0.3 0.4
Write-Wav "$OutDir\orb.wav" $orb

$n = [int]($rate * 0.18); $jump = New-Object double[] $n; $phase = 0.0
for ($i = 0; $i -lt $n; $i++) {
    $t = $i / $n
    $phase += (320 + 520 * $t) / $rate
    $jump[$i] = [Math]::Sin(2 * [Math]::PI * $phase) * 0.4 * (1.0 - $t)
}
Write-Wav "$OutDir\jump.wav" $jump

$n = [int]($rate * 0.6); $fall = New-Object double[] $n; $phase = 0.0
for ($i = 0; $i -lt $n; $i++) {
    $t = $i / $n
    $phase += (600 - 480 * $t) / $rate
    $fall[$i] = [Math]::Sin(2 * [Math]::PI * $phase) * 0.4 * (1.0 - $t)
}
Write-Wav "$OutDir\fall.wav" $fall

$win = New-Object double[] ([int]($rate * 1.2))
$k = 0
foreach ($f in 523.25, 659.25, 783.99, 1046.5) { Add-Note $win ([int]($rate * 0.14 * $k)) $f 0.5 0.4; $k++ }
Write-Wav "$OutDir\win.wav" $win

$lose = New-Object double[] ([int]($rate * 1.0))
$k = 0
foreach ($f in 392.0, 329.63, 261.63) { Add-Note $lose ([int]($rate * 0.28 * $k)) $f 0.5 0.4; $k++ }
Write-Wav "$OutDir\lose.wav" $lose
