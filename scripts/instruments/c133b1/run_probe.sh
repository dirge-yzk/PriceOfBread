#!/usr/bin/env bash
# (1291) c133b1（(1236) c125b1 の写し） 段 0：bash run_probe.sh <種の始め> <種の終わり> [月数=12]＝旗 0 と旗 1（EndowmentCostFollowsAbundance）を同じ種で（bk＝PaySalesToProducerBook は既定 1 のまま）。
S0=${1:?}; S1=${2:?}; MO=${3:-12}
W=D:/PoB_work/c133b1_work/probe; I=D:/PoB_work/wt_c133b1/scripts/instruments/c133b1
mkdir -p $W/src $W/tmp; cp $I/Probe.csproj.txt $W/src/Probe.csproj; cp $I/probe_Program.cs.txt $W/src/Program.cs
export TEMP=$W/tmp TMP=$W/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet build $W/src/Probe.csproj -c Release --artifacts-path $W/art -v q -nologo > $W/build.txt 2>&1 || { echo BUILDFAIL; tail -20 $W/build.txt; exit 1; }
EXE=$(ls $W/art/bin/Probe/release/PriceOfBread.Tests.exe)
unset POB_COEF
$EXE $S0 $S1 $MO $W/f0.tsv > $W/run_f0.log 2>&1 || { echo RUNFAIL0; exit 1; }
POB_COEF="EndowmentCostFollowsAbundance=1" $EXE $S0 $S1 $MO $W/f1.tsv > $W/run_f1.log 2>&1 || { echo RUNFAIL1; exit 1; }
echo RUNDONE
