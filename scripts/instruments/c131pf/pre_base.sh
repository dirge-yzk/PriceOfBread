#!/usr/bin/env bash
# (1277) c131pf 腕に依らない基準とヌル腕の側の読み（read_pf.sh から外した物・同じ台本で数え直す＝⑰）：bash pre_base.sh <出力フォルダ（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
OUT=${1:?出力}; K=D:/PoB_work/keep_1274; BASE=$K/base_ins
mkdir -p "$OUT"; cd "$R"
bash "$S/c78gz/stoplines906.sh" "$K/n1" "$OUT/stop906_n1.txt" 1 > "$OUT/stop906_n1.log" 2>&1; echo "stop906 n1 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$K/n1" > "$OUT/plvl_n1.txt" 2>&1; echo "plvl n1 rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" 1 > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$K/n1" 1 > "$OUT/ukdef_n1.txt" 2>&1; echo "ukdef n1 rc=$?"
python -X utf8 -I "$S/c131pf/magn_pf3.py" "$K/n1" "$BASE" 1 1 > "$OUT/magn_n1.txt" 2>&1; echo "magn n1 rc=$?"
python -X utf8 -I "$S/c131pf/xborder.py" "$K/n1" "$BASE" 1 1 > "$OUT/xborder_n1.txt" 2>&1; echo "xborder n1 rc=$?"
echo PREDONE
