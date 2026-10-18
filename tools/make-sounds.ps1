param([string]$OutDir = "$PSScriptRoot\..\samples\Platformer\Assets\sounds")

# Synthesises the Platformer sample's sound effects as 16-bit mono 22.05 kHz WAV files, so the repo needs no external audio.
#   jump.wav  - quick rising square-wave chirp
#   land.wav  - short low-passed noise thump
#   spawn.wav - two quick sine blips

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

# Jump: square wave sweeping 300 -> 750 Hz over 0.16 s with a linear fade-out.
$n = [int]($rate * 0.16); $jump = New-Object double[] $n; $phase = 0.0
for ($i = 0; $i -lt $n; $i++) {
    $t = $i / $n
    $freq = 300 + 450 * $t
    $phase += $freq / $rate
    $square = if (($phase % 1.0) -lt 0.5) { 1.0 } else { -1.0 }
    $jump[$i] = $square * 0.35 * (1.0 - $t)
}
Write-Wav "$OutDir\jump.wav" $jump

# Land: noise run through a one-pole low-pass, with a fast exponential decay.
$n = [int]($rate * 0.14); $land = New-Object double[] $n; $rng = New-Object System.Random 7; $state = 0.0
for ($i = 0; $i -lt $n; $i++) {
    $state += 0.18 * (($rng.NextDouble() * 2 - 1) - $state)
    $land[$i] = $state * 2.2 * [Math]::Exp(-$i / ($rate * 0.035))
}
Write-Wav "$OutDir\land.wav" $land

# Spawn: two sine blips (880 Hz then 1320 Hz), each with a short attack and decay.
$blip = [int]($rate * 0.07); $n = $blip * 2 + [int]($rate * 0.02); $spawn = New-Object double[] $n
for ($i = 0; $i -lt $blip; $i++) {
    $env = [Math]::Sin([Math]::PI * $i / $blip)
    $spawn[$i] = [Math]::Sin(2 * [Math]::PI * 880 * $i / $rate) * 0.4 * $env
    $spawn[$blip + [int]($rate * 0.02) + $i] = [Math]::Sin(2 * [Math]::PI * 1320 * $i / $rate) * 0.4 * $env
}
Write-Wav "$OutDir\spawn.wav" $spawn
