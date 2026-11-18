#!/usr/bin/env bash
W=D:/PoB_work/r1224_work
export TEMP=$W/tmp TMP=$W/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet build $W/src/probe2/Probe2.csproj -c Release --artifacts-path $W/art2 -v q -nologo > $W/build2.txt 2>&1 || { echo BUILDFAIL; exit 1; }
EXE=$W/art2/bin/Probe2/release/PriceOfBread.Tests.exe
unset POB_COEF
$EXE 1 ジャマイカ $W/out/p2_f0_s1.tsv || exit 1
POB_COEF="EndowmentCostFollowsAbundance=1" $EXE 1 ジャマイカ $W/out/p2_f1_s1.tsv || exit 1
echo DONE2
