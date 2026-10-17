$dll = "D:\PoB_work\sp\art\bin\PriceOfBread.Runner\release\PriceOfBread.Runner.dll"
$out = "D:\PoB_work\sp\knob_exp.txt"
"start $(Get-Date)" | Out-File $out -Encoding utf8
$cfgs = @(
  @{n="default"; e=@{}},
  @{n="Spin0"; e=@{DOTNET_ThreadPool_UnfairSemaphoreSpinLimit="0"}},
  @{n="GCc0"; e=@{DOTNET_gcConcurrent="0"}},
  @{n="Spin0GCc0"; e=@{DOTNET_ThreadPool_UnfairSemaphoreSpinLimit="0"; DOTNET_gcConcurrent="0"}},
  @{n="default"; e=@{}},
  @{n="Spin0"; e=@{DOTNET_ThreadPool_UnfairSemaphoreSpinLimit="0"}},
  @{n="GCc0"; e=@{DOTNET_gcConcurrent="0"}},
  @{n="Spin0GCc0"; e=@{DOTNET_ThreadPool_UnfairSemaphoreSpinLimit="0"; DOTNET_gcConcurrent="0"}}
)
foreach ($c in $cfgs) {
  foreach ($k in @("DOTNET_TieredPGO","DOTNET_TieredCompilation","DOTNET_ThreadPool_UnfairSemaphoreSpinLimit","DOTNET_gcConcurrent")) { Remove-Item "Env:$k" -ErrorAction SilentlyContinue }
  foreach ($k in $c.e.Keys) { Set-Item "Env:$k" $c.e[$k] }
  $sw = [Diagnostics.Stopwatch]::StartNew()
  $p = Start-Process dotnet -ArgumentList "`"$dll`" selfplay 1 200" -PassThru -NoNewWindow -RedirectStandardOutput "D:\PoB_work\sp\knob_out_$($c.n).txt"
  $p.WaitForExit()
  "$($c.n) cpu=$([int]$p.TotalProcessorTime.TotalSeconds) wall=$([int]$sw.Elapsed.TotalSeconds)" | Out-File $out -Append -Encoding utf8
}
"end $(Get-Date)" | Out-File $out -Append -Encoding utf8
