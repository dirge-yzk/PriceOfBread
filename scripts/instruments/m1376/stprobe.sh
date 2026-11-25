#!/usr/bin/env bash
# (1376) sv の (c) の確かめ：旗を 1 本ずつ 0 にした盤面でも、合わせた木の savetest の値の違いが 0 か（m1369/stprobe.sh の写し・木を引数で受ける形）。
#   bash stprobe.sh <作業フォルダ（D:/…）> <木>
WK=${1:?作業フォルダ}; R=${2:?木}; mkdir -p $WK/tmp; export TEMP=$WK/tmp TMP=$WK/tmp AVALONIA_TELEMETRY_OPTOUT=1
cd "$R" || exit 2
for f in FemalePartLawFree StateProcureAsSold; do
  POB_COEF="$f=0" SNAP_DIR=$WK/snap_$f bash $R/measure.sh $WK/savetest_$f.txt savetest 1789 300 12 0,45,75 state > $WK/stprobe_$f.log 2>&1
  echo "$f=0: $(grep -h '状態 0日の点' $WK/savetest_$f.txt | head -1) / $(grep -h 'savetest' $WK/savetest_$f.txt | tail -1)" >> $WK/stprobe.txt
  rm -rf $WK/snap_$f
done
echo PROBEDONE >> $WK/stprobe.txt
