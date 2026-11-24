#!/usr/bin/env bash
# (1335) c140hf 腕に依らない基準とヌル腕の側の読み（⑰ 同じ台本で数え直す・★(1254)・c139nc/pre_base.sh の写し・紙は pre_s0.sh）。
#   基準＝INSRAW keep_1334/base_ins・ヌル腕＝KEEPNULL keep_1334/n1・n2。bash pre_base.sh <出力フォルダ（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
OUT=${1:?出力}; K=D:/PoB_work/keep_1334; BASE=$K/base_ins
mkdir -p "$OUT"; cd "$R"
for X in base n1 n2; do P=$K/$X; [ $X = base ] && P=$BASE
  bash "$S/c78gz/stoplines906.sh" "$P" "$OUT/stop906_$X.txt" 1 > "$OUT/stop906_$X.log" 2>&1; echo "stop906 $X rc=$?"
  python -X utf8 -I "$S/brk693/plvl_seeds.py" "$P" > "$OUT/plvl_$X.txt" 2>&1; echo "plvl $X rc=$?"
  python -X utf8 -I "$S/m975/ukdef975.py" "$P" 1 > "$OUT/ukdef_$X.txt" 2>&1; echo "ukdef $X rc=$?"
  python -X utf8 "$S/hol/an_h2.py" "$P" -v4 > "$OUT/hol_v4_$X.txt" 2>&1; echo "hol $X rc=$?"
  python -X utf8 -I "$S/c140hf/migsum_hf.py" "$P" 1 10 > "$OUT/migsum_$X.txt" 2>&1; echo "migsum $X rc=$?"
done
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$BASE base=$BASE > "$OUT/gdp_zero10_base.txt" 2>&1; echo "gdpz base rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$BASE base=$BASE N1=$K/n1 > "$OUT/popjump_base.txt" 2>&1; echo "popjump base rc=$?"
echo PREDONE > "$OUT/pre.mark"
