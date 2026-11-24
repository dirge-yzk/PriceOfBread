#!/usr/bin/env bash
# (1335) c140hf 段 0 の紙（基準 keep_1334/base_ins 種 1〜10・a11 種 11〜20）。bash pre_s0.sh <出力フォルダ>
export PYTHONUTF8=1
D0="$(cd "$(dirname "$0")" && pwd)"; OUT=${1:?出力}; K=D:/PoB_work/keep_1334; mkdir -p "$OUT"
python -X utf8 -I "$D0/stage0_hf.py" $K/base_ins 1 10 ローマ=1 ジブラルタル=1 パリ=1 バーミンガム=1 > "$OUT/stage0_base.txt" 2>&1; echo "base rc=$?"
python -X utf8 -I "$D0/stage0_hf.py" $K/a11 11 10 パリ=16 バーミンガム=19 > "$OUT/stage0_a11.txt" 2>&1; echo "a11 rc=$?"
for X in n1 n2; do python -X utf8 -I "$D0/stage0_hf.py" $K/$X 1 10 > "$OUT/stage0_$X.txt" 2>&1; echo "$X rc=$?"; done
echo S0DONE > "$OUT/s0.mark"
