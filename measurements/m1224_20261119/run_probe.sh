#!/usr/bin/env bash
# (1224) c123rb：bash run_probe.sh <種の始め> <種の終わり> [月数=12]
S0=${1:?}; S1=${2:?}; MO=${3:-12}
W=D:/PoB_work/r1224_work
export TEMP=$W/tmp TMP=$W/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
EXE=$W/art/bin/Probe/release/PriceOfBread.Tests.exe
dotnet build $W/src/probe/Probe.csproj -c Release --artifacts-path $W/art -v q -nologo > $W/build.txt 2>&1 || { echo BUILDFAIL; exit 1; }
unset POB_COEF
$EXE $S0 $S1 $MO $W/f0_s${S0}_${S1} > $W/run_f0.log 2>&1 || { echo RUNFAIL0; exit 1; }
POB_COEF="EndowmentCostFollowsAbundance=1" $EXE $S0 $S1 $MO $W/f1_s${S0}_${S1} > $W/run_f1.log 2>&1 || { echo RUNFAIL1; exit 1; }
echo RUNDONE > $W/probe.done
echo RUNDONE
