#!/usr/bin/env bash
# (1231) 問い 3 のプローブ：ビルド（--artifacts-path を分ける）→ 種 1〜N。
W=D:/PoB_work/r1231_work
export TEMP=$W/tmp TMP=$W/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet build $W/src/probe/Probe.csproj -c Release --artifacts-path $W/art -v q -nologo > $W/build.txt 2>&1 || { echo BUILDFAIL; exit 1; }
EXE=$W/art/bin/Probe/release/PriceOfBread.Tests.exe
unset POB_COEF
for s in $(seq $1 $2); do $EXE $s $W/out/p_s$s.tsv > /dev/null || { echo FAIL $s; exit 1; }; done
echo DONE > $W/out/probe.done
