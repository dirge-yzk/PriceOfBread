#!/usr/bin/env bash
# (1118) c108ps 突然変異ごとの試験（使い捨ての木＝器のコミット 623750fd7 の git archive・旗は試験が自分で置く）。呼び方：bash mut_tests.sh M1 M2 ...
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
export TEMP=D:/PoB_work/c108ps_work/tmp TMP=D:/PoB_work/c108ps_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
W=D:/PoB_work/c108ps_work/mt; AP=D:/PoB_work/wt_c108ps/measurements/m1118_20261114/mut/mutapply.py
mkdir -p $W
for m in "$@"; do
  T=$W/tree_$m; rm -rf $T; mkdir -p $T
  (cd D:/PoB_work/wt_c108ps && git archive 623750fd7) | tar -x -C $T
  python -I $AP $m $T || { echo "$m MUTFAIL"; continue; }
  (cd $T && dotnet test PriceOfBread.Tests -c Release --artifacts-path $W/art_$m --filter "FullyQualifiedName~ParliamentSeatsWithoutGapTests|FullyQualifiedName~NonElectiveSeatsTests" --logger "trx;LogFileName=$W/trx_$m.trx" > $W/test_$m.log 2>&1)
  echo "== $m rc=$?"; grep -E "^\s+(失敗|Failed) |合計|Total tests" $W/test_$m.log | head -12
  rm -rf $T $W/art_$m
done
