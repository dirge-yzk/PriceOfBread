# (1439) merge: during arms M, S11 (and null arms), every 60 s write Win32_Processor LoadPercentage and the dotnet list (Get-Snap of r1419/time_a2.ps1) in ascii (override 4).
#   powershell -NoProfile -File cpu_load.ps1 <out txt> <mark 1> [<mark 2> ...]  = stops when all marks exist.
#   pacing = the 60 s Get-Counter sampling itself (-SampleInterval 5 -MaxSamples 12); no timed-wait command.
$L = $args[0]; $Marks = @($args | Select-Object -Skip 1)
function Get-Cpu { $v = @(); for ($i = 0; $i -lt 5; $i++) { $v += (Get-CimInstance Win32_Processor | Measure-Object LoadPercentage -Average).Average }; [math]::Round((($v | Measure-Object -Average).Average), 1) }
function Get-Cpu60 { $s = Get-Counter '\Processor(_Total)\% Processor Time' -SampleInterval 5 -MaxSamples 12; [math]::Round((($s.CounterSamples | Measure-Object CookedValue -Average).Average), 1) }
function Get-Snap {
  $ps = Get-CimInstance Win32_Process | Where-Object { $_.Name -match 'dotnet|testhost|PriceOfBread|csharp-ls|VBCSCompiler|MSBuild' }
  $d = @($ps | ForEach-Object {
    $cl = "$($_.CommandLine)"
    $a = if ($_.Name -eq 'dotnet.exe') { $m2 = [regex]::Matches($cl, '[A-Za-z0-9_.\-]+\.(dll|csproj|slnx|sln)'); if ($m2.Count -gt 0) { $m2[0].Value } else { ($cl -split '\s+')[1] } } else { '' }
    "$($_.Name)$(if ($a) { ':' + $a } else { '' })"
  })
  $g = $d | Group-Object | ForEach-Object { "$($_.Name)x$($_.Count)" }
  "dotnet=$($d.Count) [" + ($g -join ' ') + "]"
}
"start $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') marks=$($Marks -join ',')" | Out-File -Append -Encoding ascii $L
while ($true) {
  $left = @($Marks | Where-Object { -not (Test-Path $_) })
  if ($left.Count -eq 0) { break }
  $c60 = Get-Cpu60
  "$(Get-Date -Format 'HH:mm:ss') load=$(Get-Cpu)% cpu60=$c60% left=$($left.Count) $(Get-Snap)" | Out-File -Append -Encoding ascii $L
}
"end $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" | Out-File -Append -Encoding ascii $L
