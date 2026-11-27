# (1419) c152tq: 2 more quiet A runs (PriceOnlyStepDays 30 = default, POB_COEF empty) on tree e5db75f49 (copied from r1412/time_ab.ps1; not committed).
#   powershell -NoProfile -File time_a2.ps1 <Runner dll> <out dir>
#   Same command as (1412): selfplay-batch 1, POB_TIME=1. Labels are ASCII only.
#   Quiet check at begin: 5-sample CIM average; if > 10% or a foreign build/test process is present, re-measure as a 60 s Get-Counter average (max 3).
#   During the run: every 20 s, one Get-Dot line to load.txt (WaitForExit(20000) on the process itself = returns at exit).
#   NOT_QUIET = begin load > 10% after retries, OR a foreign MSBuild / testhost / Tests.csproj / other Runner seen at any sample of the run.
#   Each NOT_QUIET run may be re-run once (total runs <= 4).
param([string]$dll, [string]$T)
$ErrorActionPreference = 'Stop'
$L = "$T/load.txt"
function Get-Cpu { $v = @(); for ($i = 0; $i -lt 5; $i++) { $v += (Get-CimInstance Win32_Processor | Measure-Object LoadPercentage -Average).Average }; [math]::Round((($v | Measure-Object -Average).Average), 1) }
function Get-Cpu60 { $s = Get-Counter '\Processor(_Total)\% Processor Time' -SampleInterval 5 -MaxSamples 12; [math]::Round((($s.CounterSamples | Measure-Object CookedValue -Average).Average), 1) }
$script:mypid = -1
function Get-Snap {
  $ps = Get-CimInstance Win32_Process | Where-Object { $_.Name -match 'dotnet|testhost|PriceOfBread|csharp-ls|VBCSCompiler|MSBuild' }
  $foreign = @()
  $d = @($ps | ForEach-Object {
    $cl = "$($_.CommandLine)"
    $a = if ($_.Name -eq 'dotnet.exe') { $m2 = [regex]::Matches($cl, '[A-Za-z0-9_.\-]+\.(dll|csproj|slnx|sln)'); if ($m2.Count -gt 0) { $m2[0].Value } else { ($cl -split '\s+')[1] } } else { '' }
    $nm = "$($_.Name)$(if ($a) { ':' + $a } else { '' })"
    if ($_.ProcessId -ne $script:mypid) {
      if ($_.Name -match 'MSBuild|testhost' -or $cl -match 'MSBuild\.dll|testhost|Tests\.csproj|Tests\.Ui\.csproj|PriceOfBread\.Tests|vstest' -or $a -match 'Runner\.dll') { $foreign += $nm }
    }
    $nm
  })
  $g = $d | Group-Object | ForEach-Object { "$($_.Name)x$($_.Count)" }
  [pscustomobject]@{ Text = "dotnet=$($d.Count) [" + ($g -join ' ') + "]"; Foreign = $foreign }
}
function Write-Load([string]$tag, [string]$c, $s) {
  $f = if ($s.Foreign.Count -gt 0) { " FOREIGN=[" + (($s.Foreign | Group-Object | ForEach-Object { "$($_.Name)x$($_.Count)" }) -join ' ') + "]" } else { '' }
  "$(Get-Date -Format 'HH:mm:ss') $tag cpu=$c $($s.Text)$f" | Out-File -Append -Encoding ascii $L
}
"start $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') dll=$dll" | Out-File -Encoding ascii $L
"env POB_*: [" + ((Get-ChildItem Env: | Where-Object { $_.Name -like 'POB_*' } | ForEach-Object { "$($_.Name)=$($_.Value)" }) -join ' ') + "]" | Out-File -Append -Encoding ascii $L
$queue = New-Object System.Collections.Generic.List[object]
$queue.Add(@{ id = 1; rerun = $false }); $queue.Add(@{ id = 2; rerun = $false })
$k = 0; $nq = 0; $qi = 0
while ($qi -lt $queue.Count -and $k -lt 4) {
  $item = $queue[$qi]; $qi++
  $k++
  $name = "r$($k)A"
  $script:mypid = -1
  $c = Get-Cpu; $s = Get-Snap; Write-Load "$name begin" "$c%" $s
  $tries = 0
  while (($c -gt 10 -or $s.Foreign.Count -gt 0) -and $tries -lt 3) { $tries++; $c = Get-Cpu60; $s = Get-Snap; Write-Load "$name begin-retry$tries(60s avg)" "$c%" $s }
  $nqr = @()
  if ($c -gt 10) { $nqr += 'beginload' }
  if ($s.Foreign.Count -gt 0) { $nqr += 'foreign@begin' }
  $env:POB_TIME = '1'; $env:POB_COST_OUT = "$T/$name.txt.cost"
  Remove-Item Env:POB_COEF -ErrorAction SilentlyContinue
  $t0 = Get-Date
  $p = Start-Process dotnet -ArgumentList $dll, 'selfplay-batch', '1' -RedirectStandardOutput "$T/$name.txt" -RedirectStandardError "$T/$name.err" -PassThru -NoNewWindow
  $null = $p.Handle
  $script:mypid = $p.Id
  $tick = 0
  while (-not $p.WaitForExit(20000)) {
    $tick++
    $s = Get-Snap
    $cc = if ($tick -eq 5) { "$(Get-Cpu)%" } else { '-' }
    Write-Load "$name mid$tick" $cc $s
    if ($s.Foreign.Count -gt 0 -and -not ($nqr -contains 'foreign@mid')) { $nqr += 'foreign@mid' }
  }
  $p.WaitForExit()
  $wall = [math]::Round(((Get-Date) - $t0).TotalSeconds, 1)
  $cpu = [math]::Round($p.TotalProcessorTime.TotalSeconds, 1)
  $script:mypid = -1
  $s = Get-Snap; Write-Load "$name end" "$(Get-Cpu)%" $s
  if ($s.Foreign.Count -gt 0) { $nqr += 'foreign@end' }
  $quiet = if ($nqr.Count -gt 0) { 'NOT_QUIET' } else { 'quiet' }
  if ($quiet -eq 'quiet') { $nq++ }
  "$name combo=A coef=[] rc=$($p.ExitCode) wall=$wall cpu=$cpu $quiet $(if ($nqr.Count) { '(' + ($nqr -join ',') + ')' }) rerunOf=$(if ($item.rerun) { $item.of } else { '-' })" | Out-File -Append -Encoding ascii $L
  if ($quiet -eq 'NOT_QUIET' -and -not $item.rerun) { $queue.Add(@{ id = $k; rerun = $true; of = $name }) }
}
Remove-Item Env:POB_COEF -ErrorAction SilentlyContinue
"done $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') runs=$k quiet=$nq" | Out-File -Append -Encoding ascii $L
