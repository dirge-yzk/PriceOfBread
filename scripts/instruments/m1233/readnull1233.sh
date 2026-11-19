#!/usr/bin/env bash
# (1233) 合流（m1226/readnull1226.sh の写し・木と作業フォルダを wt_m1233・m1233_work へ）
# (1226) 合流のヌル腕 N1（木 T）の記録の読み（空洞化・英の国債の不履行・名目 GDP 0 の続き・止める線の数）。
#   bash readnull1226.sh <ins（D:/ の形）> <旧のヌル腕の生> <出力>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; OLD=${2:?旧}; OUT=${3:?出力}; mkdir -p "$OUT"; cd "$R"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" 1 > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$OLD > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" 1 > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
echo READDONE
