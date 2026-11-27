# (1404) 合流：速さの計器（StageTimer）を開いた素の 1 種（種 1・selfplay-batch 1・POB_TIME=1）＝m1397/time3.ps1 の写し（dotnet-counters は付けない）。
#   powershell -NoProfile -File time_1404.ps1 <Runner の dll> <出力フォルダ>
param([string]$dll, [string]$T)
[Console]::OutputEncoding=[Text.Encoding]::UTF8
function Get-Load { $c=(Get-CimInstance Win32_Processor|Measure-Object LoadPercentage -Average).Average; $n=(Get-Process|Where-Object {$_.ProcessName -match 'dotnet|testhost|PriceOfBread|csharp-ls|VBCSCompiler|MSBuild'}|Measure-Object).Count; "$(Get-Date -Format 'HH:mm:ss') 負荷 $c% dotnet系 $n 本" }
"始め $(Get-Load)" | Out-File -Encoding utf8 "$T/load.txt"
$env:POB_TIME='1'; $env:POB_COST_OUT="$T/b1.txt.cost"
$t0=Get-Date
$p=Start-Process dotnet -ArgumentList $dll,'selfplay-batch','1' -RedirectStandardOutput "$T/b1.txt" -RedirectStandardError "$T/err.txt" -PassThru -NoNewWindow
"pid $($p.Id)" | Out-File -Append -Encoding utf8 "$T/load.txt"
$p.WaitForExit()   # 途中の負荷は別の命令で 1 回だけ取る（時間で待つ繰り返しを書かない）
"終わり $(Get-Load)" | Out-File -Append -Encoding utf8 "$T/load.txt"
"rc=$($p.ExitCode) wall=$([math]::Round(((Get-Date)-$t0).TotalSeconds,1)) cpu=$([math]::Round($p.TotalProcessorTime.TotalSeconds,1))" | Out-File -Append -Encoding utf8 "$T/load.txt"
