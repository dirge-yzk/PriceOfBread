#!/usr/bin/env bash
# (300) 線 i1：記録の列を一括で読む（読むだけ）。使い方：bash rec.sh <腕名> <ins> <出力フォルダ> [種の数=10]
#   出す物：l4w（英の兌換停止・不履行の窓）・supply_n（補給崩壊）・an_h6（歳入÷年GDP・債務÷年GDP）・binder_split（払えない月・満額借りても足りない月）・an_h2（空洞化 -v4）・an_d289（粗投資の総額 a・州の偏り b）
export PYTHONUTF8=1
ARM=$1; INS=$2; OUT=$3; N=${4:-10}; R=$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)
mkdir -p "$OUT"
python $R/measurements/rev1_20261012_readers/l4w.py "$INS" > "$OUT/${ARM}_l4w.txt" 2>&1
python $R/measurements/rev1_20261012_readers/supply_n.py "$INS" $N > "$OUT/${ARM}_supply.txt" 2>&1
python $R/scripts/instruments/h6/an_h6.py $ARM="$INS" > "$OUT/${ARM}_h6.txt" 2>&1
python $R/scripts/instruments/h6/binder_split.py "$INS" 6 40 > "$OUT/${ARM}_binder.txt" 2>&1
python $R/scripts/instruments/hol/an_h2.py "$INS" -v4 -ex old > "$OUT/${ARM}_hollow.txt" 2>&1
mkdir -p "$OUT/${ARM}_d289" && python $R/scripts/instruments/d289/an_d289.py "$INS" "$OUT/${ARM}_d289" a b > /dev/null 2>&1
echo REC_DONE $ARM
