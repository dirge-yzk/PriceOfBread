# (1405) c150tm supplement: A B C D started at the same moment (same background load for all four combos).
#   powershell -NoProfile -File time4par.ps1 <Runner dll> <out dir>
#   Each process gets its own POB_COEF via a per-process environment (set before Start-Process, read at process start).
param([string]$dll, [string]$T)
$ErrorActionPreference = 'Stop'
$combos = [ordered]@{ A = ''; B = 'PriceOnlyStepDays=15'; C = 'RandomOnlyClimateEpidemic=0'; D = 'PriceOnlyStepDays=15;RandomOnlyClimateEpidemic=0' }
$L = "$T/load.txt"
function Get-Cpu { $v = @(); for ($i = 0; $i -lt 5; $i++) { $v += (Get-CimInstance Win32_Processor | Measure-Object LoadPercentage -Average).Average }; [math]::Round((($v | Measure-Object -Average).Average), 1) }
function Get-Ndot { (Get-Process | Where-Object { $_.ProcessName -match 'dotnet|testhost|PriceOfBread|csharp-ls|VBCSCompiler|MSBuild' } | Measure-Object).Count }
function Write-Load([string]$tag) { $c = Get-Cpu; $n = Get-Ndot; "$(Get-Date -Format 'HH:mm:ss') $tag cpu=$c% dotnet=$n" | Out-File -Append -Encoding ascii $L }
"start $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') dll=$dll (4 parallel)" | Out-File -Encoding ascii $L
Write-Load "p begin"
$env:POB_TIME = '1'
$ps = @{}; $t0 = @{}
foreach ($g in $combos.Keys) {
  $name = "p$g"
  $env:POB_COST_OUT = "$T/$name.txt.cost"
  if ($combos[$g] -eq '') { Remove-Item Env:POB_COEF -ErrorAction SilentlyContinue } else { $env:POB_COEF = $combos[$g] }
  $t0[$g] = Get-Date
  $ps[$g] = Start-Process dotnet -ArgumentList $dll, 'selfplay-batch', '1' -RedirectStandardOutput "$T/$name.txt" -RedirectStandardError "$T/$name.err" -PassThru -NoNewWindow
  $null = $ps[$g].Handle
}
Remove-Item Env:POB_COEF -ErrorAction SilentlyContinue
if (-not $ps['A'].WaitForExit(100000)) { Write-Load "p mid" }
foreach ($g in $combos.Keys) {
  $ps[$g].WaitForExit()
  $wall = [math]::Round(($ps[$g].ExitTime - $t0[$g]).TotalSeconds, 1)
  $cpu = [math]::Round($ps[$g].TotalProcessorTime.TotalSeconds, 1)
  "p$g combo=$g coef=[$($combos[$g])] rc=$($ps[$g].ExitCode) wall=$wall cpu=$cpu parallel" | Out-File -Append -Encoding ascii $L
}
Write-Load "p end"
"done $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') runs=4" | Out-File -Append -Encoding ascii $L
