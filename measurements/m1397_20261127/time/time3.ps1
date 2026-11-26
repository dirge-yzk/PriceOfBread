[Console]::OutputEncoding=[Text.Encoding]::UTF8
$T='D:/PoB_work/m1397_work/time'
function Get-Load { $c=(Get-CimInstance Win32_Processor|Measure-Object LoadPercentage -Average).Average; $n=(Get-Process|Where-Object {$_.ProcessName -match 'dotnet|testhost|PriceOfBread|csharp-ls|VBCSCompiler|MSBuild'}|Measure-Object).Count; "$(Get-Date -Format 'HH:mm:ss') 負荷 $c% dotnet系 $n 本" }
"始め $(Get-Load)" | Out-File -Encoding utf8 "$T/load.txt"
$env:POB_TIME='1'; $env:POB_COST_OUT="$T/b1.txt.cost"
$dll='D:/PoB_work/m1397_art/bin/PriceOfBread.Runner/release/PriceOfBread.Runner.dll'
$t0=Get-Date
$p=Start-Process dotnet -ArgumentList $dll,'selfplay-batch','1' -RedirectStandardOutput "$T/b1.txt" -RedirectStandardError "$T/err.txt" -PassThru -NoNewWindow
$c=Start-Process dotnet-counters -ArgumentList 'collect','-p',$p.Id,'--counters','System.Runtime','--format','csv','--refresh-interval','5','-o',"$T/counters.csv" -RedirectStandardOutput "$T/ctr.log" -PassThru -NoNewWindow
"pid $($p.Id)" | Out-File -Append -Encoding utf8 "$T/load.txt"
$p.WaitForExit()
$c.WaitForExit()
"終わり $(Get-Load)" | Out-File -Append -Encoding utf8 "$T/load.txt"
"rc=$($p.ExitCode) wall=$([math]::Round(((Get-Date)-$t0).TotalSeconds,1))" | Out-File -Append -Encoding utf8 "$T/load.txt"
