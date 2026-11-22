#!/usr/bin/env bash
# (1297) c134bi 段 0：bash run_probe.sh <種の始め> <種の終わり>
#   f0＝旗 0・f1＝旗 1（EndowmentCostFollowsAbundance=1）・f1L＝旗 1 で助走 120 四半期・f1s＝旗 1 で銀の Scale を播種の後に ×0.25（初期値の揺らし）。
#   どれも OpeningBakeEnabled は既定 0（表は読まれない）。f1B＝旗 1 で OpeningBakeEnabled=1（今の表＝旗 0 で焼いた物を読む・比べの対照）。
S0=${1:?}; S1=${2:?}
W=D:/PoB_work/c134bi_work/probe; I=D:/PoB_work/wt_c134bi/scripts/instruments/c134bi
mkdir -p $W/src $W/tmp; cp $I/Probe.csproj.txt $W/src/Probe.csproj; cp $I/probe_Program.cs.txt $W/src/Program.cs
export TEMP=$W/tmp TMP=$W/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet build $W/src/Probe.csproj -c Release --artifacts-path $W/art -v q -nologo > $W/build.txt 2>&1 || { echo BUILDFAIL; tail -20 $W/build.txt; exit 1; }
EXE=$(ls $W/art/bin/Probe/release/PriceOfBread.Tests.exe)
unset POB_COEF
$EXE $S0 $S1 20 8 1 $W/f0.tsv > $W/run_f0.log 2>&1 || { echo RUNFAIL0; exit 1; }
POB_COEF="EndowmentCostFollowsAbundance=1" $EXE $S0 $S1 20 8 1 $W/f1.tsv > $W/run_f1.log 2>&1 || { echo RUNFAIL1; exit 1; }
POB_COEF="EndowmentCostFollowsAbundance=1" $EXE $S0 $S1 20 8 0.25 $W/f1s.tsv > $W/run_f1s.log 2>&1 || { echo RUNFAIL1s; exit 1; }
POB_COEF="EndowmentCostFollowsAbundance=1;OpeningBakeEnabled=1" $EXE $S0 $S1 20 8 1 $W/f1B.tsv > $W/run_f1B.log 2>&1 || { echo RUNFAIL1B; exit 1; }
POB_COEF="EndowmentCostFollowsAbundance=1" $EXE $S0 $S1 120 8 1 $W/f1L.tsv > $W/run_f1L.log 2>&1 || { echo RUNFAIL1L; exit 1; }
echo RUNDONE
