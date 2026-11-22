#!/usr/bin/env bash
# (1297) c134bi 段 0 の 2：旗 1 の世界で開幕の表を焼き直し（既存の道 scripts/instruments/hz4_490/bake.sh・Runner の bakeopen）、その表を読んだ盤面の銀・金・鉄鉱石・石炭をプローブで読む。
#   bash run_bake.sh  → D:/PoB_work/c134bi_work/bake_f1.json（＋.series.tsv）・probe/f1Bn.tsv（旗 1＋新しい表）・probe/f0Bo.tsv（旗 0＋今の表＝対照）
W=D:/PoB_work/c134bi_work; I=D:/PoB_work/wt_c134bi/scripts/instruments
export POB_WORK=$W/bakework
POB_COEF="EndowmentCostFollowsAbundance=1" bash $I/hz4_490/bake.sh $W/bake_f1.json 1 200 || { echo BAKEFAIL; exit 1; }
cp $I/c134bi/probe_Program.cs.txt $W/probe/src/Program.cs
TEMP=$W/probe/tmp TMP=$W/probe/tmp dotnet build $W/probe/src/Probe.csproj -c Release --artifacts-path $W/probe/art -v q -nologo > $W/probe/build2.txt 2>&1 || { echo BUILDFAIL; exit 1; }
EXE=$(ls $W/probe/art/bin/Probe/release/PriceOfBread.Tests.exe)
export TEMP=$W/probe/tmp TMP=$W/probe/tmp
POB_COEF="EndowmentCostFollowsAbundance=1" $EXE 1 3 20 8 1 $W/probe/f1Bn.tsv $W/bake_f1.json > $W/probe/run_f1Bn.log 2>&1 || { echo RUNFAIL1Bn; exit 1; }
unset POB_COEF
$EXE 1 3 20 8 1 $W/probe/f0Bo.tsv D:/PoB_work/wt_c134bi/PriceOfBread.SimCore/opening_bake.json > $W/probe/run_f0Bo.log 2>&1 || { echo RUNFAIL0Bo; exit 1; }
echo RUNDONE
