$dll = "D:\PoB_work\sp\art\bin\PriceOfBread.Runner\release\PriceOfBread.Runner.dll"
$out = "D:\PoB_work\sp\jit_exp.txt"
"start $(Get-Date)" | Out-File $out -Encoding utf8
$cfgs = @(
  @{n="default"; e=@{}},
  @{n="PGO0"; e=@{DOTNET_TieredPGO="0"}},
  @{n="TC0"; e=@{DOTNET_TieredCompilation="0"}},
  @{n="default"; e=@{}},
  @{n="PGO0"; e=@{DOTNET_TieredPGO="0"}},
  @{n="TC0"; e=@{DOTNET_TieredCompilation="0"}}
)
foreach ($c in $cfgs) {
  foreach ($k in @("DOTNET_TieredPGO","DOTNET_TieredCompilation")) { Remove-Item "Env:$k" -ErrorAction SilentlyContinue }
  foreach ($k in $c.e.Keys) { Set-Item "Env:$k" $c.e[$k] }
  $sw = [Diagnostics.Stopwatch]::StartNew()
  $p = Start-Process dotnet -ArgumentList "`"$dll`" selfplay 1 200" -PassThru -NoNewWindow -RedirectStandardOutput "D:\PoB_work\sp\jit_out_$($c.n).txt"
  $p.WaitForExit()
  "$($c.n) cpu=$([int]$p.TotalProcessorTime.TotalSeconds) wall=$([int]$sw.Elapsed.TotalSeconds)" | Out-File $out -Append -Encoding utf8
}
"end $(Get-Date)" | Out-File $out -Append -Encoding utf8
