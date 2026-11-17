#!/usr/bin/env bash
# (1195) c119us 突然変異の腕（使い捨ての木 D:/PoB_work/wt_c119usx を判定の 1 本化のコミットへ戻し、変異ごとに T1〜T3 を回す）。bash mut.sh <コミット> <出力フォルダ>
C=${1:?コミット}; OUT=${2:?出力}; X=D:/PoB_work/wt_c119usx; S="$(cd "$(dirname "$0")" && pwd)"
export TEMP=D:/PoB_work/c119us_work/tmp TMP=D:/PoB_work/c119us_work/tmp
for m in M1 M2 M3 M4 M5; do
  git -C $X checkout -q -- . && git -C $X checkout -q --detach $C || exit 1
  python -X utf8 -I $S/mut.py $X $m || exit 2
  bash D:/PoB_work/c119us_work/t1.sh $X D:/PoB_work/c119us_work/artx "FullyQualifiedName~UnificationWarGateTests" $OUT/mut_$m.txt
  echo "$m $(grep -o '失敗:  *[0-9]*、合格:  *[0-9]*' $OUT/mut_$m.txt) $(grep -o 'T[123]_' $OUT/mut_$m.txt | sort | tr '\n' ' ')"
done
git -C $X checkout -q -- .
