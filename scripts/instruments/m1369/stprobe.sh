#!/usr/bin/env bash
# (1369) savetest の 303（Producer.EffGoods の共有の形）の帰属：旗を 1 本ずつ 0 にして savetest を回す（エンジンは直さない・記録だけ）。
#   bash scripts/instruments/m1369/stprobe.sh <作業フォルダ（D:/…）>
WK=${1:?作業フォルダ}; mkdir -p $WK/tmp; export TEMP=$WK/tmp TMP=$WK/tmp AVALONIA_TELEMETRY_OPTOUT=1
cd "$(dirname "$0")/../../.."; R=$(pwd)
for f in FemalePartLawFree StateProcureAsSold; do
  POB_COEF="$f=0" SNAP_DIR=$WK/snap_$f bash $R/measure.sh $WK/savetest_$f.txt savetest 1789 300 12 0,45,75 state > $WK/stprobe_$f.log 2>&1
  echo "$f=0: $(grep -h '状態 0日の点' $WK/savetest_$f.txt | head -1) / $(grep -h 'savetest' $WK/savetest_$f.txt | tail -1)" >> $WK/stprobe.txt
  rm -rf $WK/snap_$f
done
echo PROBEDONE >> $WK/stprobe.txt
