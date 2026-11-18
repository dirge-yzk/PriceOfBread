#!/usr/bin/env bash
# (1222) c123k3 段0 のプローブ：ビルドして種 1〜5 を別のプロセスで同時に回す（bg.sh で起こす）。
W=D:/PoB_work/c123k3_work; export TEMP=$W/tmp TMP=$W/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
unset POB_COEF
dotnet build $W/probe/Probe.csproj -c Release --artifacts-path $W/art -v q -nologo > $W/build.txt 2>&1 || { echo BUILDFAIL; exit 1; }
EXE=$W/art/bin/Probe/release/PriceOfBread.Tests.exe
for s in 1 2 3 4 5 6 7 8 9 10; do echo "$EXE $s 668 $W/k3m_s$s.tsv"; done | xargs -P 10 -I{} sh -c '{}'
ls $W/k3m_s*.tsv | wc -l
