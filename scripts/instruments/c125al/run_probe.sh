#!/usr/bin/env bash
# (1237) 段 0 のプローブ：ビルド→旗 0 と旗 1 で種 1〜10。
W=D:/PoB_work/c125al_work/probe
export TEMP=$W/tmp TMP=$W/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet build $W/src/probe/Probe.csproj -c Release --artifacts-path $W/art -v q -nologo > $W/build.txt 2>&1 || { echo BUILDFAIL > $W/out/probe.done; exit 1; }
EXE=$W/art/bin/Probe/release/PriceOfBread.Tests.exe
for s in $(seq 1 10); do
  unset POB_COEF; $EXE $s $W/out/f0_s$s.tsv > /dev/null || echo FAIL0 $s >> $W/out/fail.txt
  POB_COEF=FoodProcessingNoEngineer=1 $EXE $s $W/out/f1_s$s.tsv > /dev/null || echo FAIL1 $s >> $W/out/fail.txt
done
echo DONE > $W/out/probe.done
