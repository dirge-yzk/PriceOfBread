# (1412) c151tq: timing of pm (PriceOnlyStepDays 30 vs 15), 2 runs each, order A B A B (copied from r1405/time2x2.ps1).
#   powershell -NoProfile -File time_ab.ps1 <Runner dll> <out dir>
#   Same command as measurements/m1404_20261127/time (selfplay-batch 1, POB_TIME=1). Labels are ASCII only.
#   Quiet check: begin load is a 5-sample CIM average; if > 10%, re-measure as a 60 s Get-Counter average (max 3 re-measures, total < 240 s).
#   Mid sample: one WaitForExit(100000) on the process itself (returns early on exit), then WaitForExit().
param([string]$dll, [string]$T)
$ErrorActionPreference = 'Stop'
$combos = [ordered]@{ A = ''; B = 'PriceOnlyStepDays=15' }
$order = @('A','B','A','B')
$L = "$T/load.txt"
function Get-Cpu { $v = @(); for ($i = 0; $i -lt 5; $i++) { $v += (Get-CimInstance Win32_Processor | Measure-Object LoadPercentage -Average).Average }; [math]::Round((($v | Measure-Object -Average).Average), 1) }
function Get-Cpu60 { $s = Get-Counter '\Processor(_Total)\% Processor Time' -SampleInterval 5 -MaxSamples 12; [math]::Round((($s.CounterSamples | Measure-Object CookedValue -Average).Average), 1) }
function Get-Dot {
  $ps = Get-CimInstance Win32_Process | Where-Object { $_.Name -match 'dotnet|testhost|PriceOfBread|csharp-ls|VBCSCompiler|MSBuild' }
  $d = @($ps | ForEach-Object {
    $cl = "$($_.CommandLine)"; $m = [regex]::Match($cl, '([A-Za-z0-9_.\-]+\.(dll|exe|csproj|slnx|sln))(?=["\s]|$)', 'RightToLeft')
    $a = if ($_.Name -eq 'dotnet.exe') { $m2 = [regex]::Matches($cl, '[A-Za-z0-9_.\-]+\.(dll|csproj|slnx|sln)'); if ($m2.Count -gt 0) { $m2[0].Value } else { ($cl -split '\s+')[1] } } else { '' }
    "$($_.Name)$(if ($a) { ':' + $a } else { '' })"
  })
  $g = $d | Group-Object | ForEach-Object { "$($_.Name)x$($_.Count)" }
  "dotnet=$($d.Count) [" + ($g -join ' ') + "]"
}
function Write-Load([string]$tag, [double]$c) { "$(Get-Date -Format 'HH:mm:ss') $tag cpu=$c% $(Get-Dot)" | Out-File -Append -Encoding ascii $L }
"start $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') dll=$dll" | Out-File -Encoding ascii $L
$k = 0; $nq = 0
foreach ($g in $order) {
  $k++
  $name = "r$k$g"
  $c = Get-Cpu; Write-Load "$name begin" $c
  $tries = 0
  while ($c -gt 10 -and $tries -lt 3) { $tries++; $c = Get-Cpu60; Write-Load "$name begin-retry$tries(60s avg)" $c }
  $quiet = if ($c -gt 10) { 'NOT_QUIET' } else { 'quiet' }
  if ($quiet -eq 'quiet') { $nq++ }
  $env:POB_TIME = '1'; $env:POB_COST_OUT = "$T/$name.txt.cost"
  if ($combos[$g] -eq '') { Remove-Item Env:POB_COEF -ErrorAction SilentlyContinue } else { $env:POB_COEF = $combos[$g] }
  $t0 = Get-Date
  $p = Start-Process dotnet -ArgumentList $dll, 'selfplay-batch', '1' -RedirectStandardOutput "$T/$name.txt" -RedirectStandardError "$T/$name.err" -PassThru -NoNewWindow
  $null = $p.Handle
  if (-not $p.WaitForExit(100000)) { Write-Load "$name mid" (Get-Cpu) }
  $p.WaitForExit()
  $wall = [math]::Round(((Get-Date) - $t0).TotalSeconds, 1)
  $cpu = [math]::Round($p.TotalProcessorTime.TotalSeconds, 1)
  Write-Load "$name end" (Get-Cpu)
  "$name combo=$g coef=[$($combos[$g])] rc=$($p.ExitCode) wall=$wall cpu=$cpu $quiet" | Out-File -Append -Encoding ascii $L
}
Remove-Item Env:POB_COEF -ErrorAction SilentlyContinue
"done $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') runs=$k quiet=$nq" | Out-File -Append -Encoding ascii $L
