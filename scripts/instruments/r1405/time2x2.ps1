# (1405) c150tm: 2x2 timing of pm (PriceOnlyStepDays) x rn (RandomOnlyClimateEpidemic), 2 runs each, order A B C D A B C D.
#   powershell -NoProfile -File time2x2.ps1 <Runner dll> <out dir>
#   Same command as measurements/m1404_20261127/time (selfplay-batch 1, POB_TIME=1). Labels are ASCII only (no mojibake).
#   Mid sample: one WaitForExit(100000) on the process itself (returns early on exit), then WaitForExit().
param([string]$dll, [string]$T)
$ErrorActionPreference = 'Stop'
$combos = [ordered]@{ A = ''; B = 'PriceOnlyStepDays=15'; C = 'RandomOnlyClimateEpidemic=0'; D = 'PriceOnlyStepDays=15;RandomOnlyClimateEpidemic=0' }
$order = @('A','B','C','D','A','B','C','D')
$L = "$T/load.txt"
function Get-Cpu { $v = @(); for ($i = 0; $i -lt 5; $i++) { $v += (Get-CimInstance Win32_Processor | Measure-Object LoadPercentage -Average).Average }; [math]::Round((($v | Measure-Object -Average).Average), 1) }
function Get-Ndot { (Get-Process | Where-Object { $_.ProcessName -match 'dotnet|testhost|PriceOfBread|csharp-ls|VBCSCompiler|MSBuild' } | Measure-Object).Count }
function Write-Load([string]$tag) { $c = Get-Cpu; $n = Get-Ndot; "$(Get-Date -Format 'HH:mm:ss') $tag cpu=$c% dotnet=$n" | Out-File -Append -Encoding ascii $L; return $c }
"start $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') dll=$dll" | Out-File -Encoding ascii $L
$k = 0
foreach ($g in $order) {
  $k++
  $name = "r$k$g"
  $c = Write-Load "$name begin"
  $tries = 0
  while ($c -gt 10 -and $tries -lt 3) { $tries++; $c = Write-Load "$name begin-retry$tries" }
  $quiet = if ($c -gt 10) { 'NOT_QUIET' } else { 'quiet' }
  $env:POB_TIME = '1'; $env:POB_COST_OUT = "$T/$name.txt.cost"
  if ($combos[$g] -eq '') { Remove-Item Env:POB_COEF -ErrorAction SilentlyContinue } else { $env:POB_COEF = $combos[$g] }
  $t0 = Get-Date
  $p = Start-Process dotnet -ArgumentList $dll, 'selfplay-batch', '1' -RedirectStandardOutput "$T/$name.txt" -RedirectStandardError "$T/$name.err" -PassThru -NoNewWindow
  $null = $p.Handle
  if (-not $p.WaitForExit(100000)) { $null = Write-Load "$name mid" }
  $p.WaitForExit()
  $wall = [math]::Round(((Get-Date) - $t0).TotalSeconds, 1)
  $cpu = [math]::Round($p.TotalProcessorTime.TotalSeconds, 1)
  $null = Write-Load "$name end"
  "$name combo=$g coef=[$($combos[$g])] rc=$($p.ExitCode) wall=$wall cpu=$cpu $quiet" | Out-File -Append -Encoding ascii $L
}
Remove-Item Env:POB_COEF -ErrorAction SilentlyContinue
"done $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') runs=$k" | Out-File -Append -Encoding ascii $L
