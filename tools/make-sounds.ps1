param([string]$OutDir = "$PSScriptRoot\..\samples\Platformer\Assets\sounds")

# Synthesises the Platformer sample's sound effects as 16-bit mono 22.05 kHz WAV files, so the repo needs no external audio.
#   jump.wav  - quick rising square-wave chirp
#   land.wav  - short low-passed noise thump
#   spawn.wav - two quick sine blips
#   coin.wav  - bright two-note pickup chime
#   hurt.wav  - descending buzz
#   stomp.wav - short pop
#   win.wav   - rising arpeggio
#   lose.wav  - slow falling tone
#   unlock.wav - rising chirp when the exit opens

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

# Tone helper: sine (or square) at a frequency that glides from $f0 to $f1, with an attack/decay envelope, added into $buf at $start.
function Add-Tone([double[]]$buf, [int]$start, [double]$seconds, [double]$f0, [double]$f1, [double]$gain, [bool]$square) {
    $n = [int]($rate * $seconds); $phase = 0.0
    for ($i = 0; $i -lt $n -and ($start + $i) -lt $buf.Length; $i++) {
        $t = $i / $n
        $phase += ($f0 + ($f1 - $f0) * $t) / $rate
        $wave = if ($square) { if (($phase % 1.0) -lt 0.5) { 1.0 } else { -1.0 } } else { [Math]::Sin(2 * [Math]::PI * $phase) }
        $env = [Math]::Min(1.0, $i / ($rate * 0.005)) * [Math]::Pow(1.0 - $t, 1.5)
        $buf[$start + $i] += $wave * $gain * $env
    }
}

$coin = New-Object double[] ([int]($rate * 0.3))
Add-Tone $coin 0 0.08 988 988 0.35 $false
Add-Tone $coin ([int]($rate * 0.07)) 0.22 1319 1319 0.35 $false
Write-Wav "$OutDir\coin.wav" $coin

$hurt = New-Object double[] ([int]($rate * 0.35))
Add-Tone $hurt 0 0.35 330 90 0.4 $true
Write-Wav "$OutDir\hurt.wav" $hurt

$stomp = New-Object double[] ([int]($rate * 0.12))
Add-Tone $stomp 0 0.12 420 120 0.5 $false
Write-Wav "$OutDir\stomp.wav" $stomp

$win = New-Object double[] ([int]($rate * 0.9))
$notes = @(523, 659, 784, 1047)
for ($k = 0; $k -lt 4; $k++) { Add-Tone $win ([int]($rate * 0.14 * $k)) 0.4 $notes[$k] $notes[$k] 0.3 $false }
Write-Wav "$OutDir\win.wav" $win

$lose = New-Object double[] ([int]($rate * 0.9))
Add-Tone $lose 0 0.9 392 130 0.35 $true
Write-Wav "$OutDir\lose.wav" $lose

$unlock = New-Object double[] ([int]($rate * 0.5))
Add-Tone $unlock 0 0.18 660 660 0.3 $false
Add-Tone $unlock ([int]($rate * 0.15)) 0.35 990 1320 0.3 $false
Write-Wav "$OutDir\unlock.wav" $unlock
