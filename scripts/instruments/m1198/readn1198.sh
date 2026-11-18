#!/usr/bin/env bash
# (1198) 合流：新しいヌル腕 N1 の記録の読み（門にしない）＝空洞化 1/10 未満・名目 GDP 0 の続き・英の国債の不履行・価格水準・NG。
#   bash readn1198.sh <N1 の ins（D:/ の形）> <出力フォルダ（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; OUT=${2:?出力}; K=D:/PoB_work/keep_1191
mkdir -p "$OUT"; cd "$R"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$K/n1 > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" 1 > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$K/n1" 1 > "$OUT/ukdef_n1old.txt" 2>&1; echo "ukdef n1old rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" 1 > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
bash "$S/ng.sh" "$INS" n1198 "$OUT/ng" rd:$R/measurements/ng1191_20261118:m1191 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
echo READDONE
