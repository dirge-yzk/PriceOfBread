#!/usr/bin/env bash
# (1362) 合流：ヌル腕 N1・N2（木 98d7a5283・SupplySlope 0.50000001／0.49999999）の記録の読み手（止める線・空洞化・名目 GDP 0 の続き・英の不履行・NG）。
#   使い方: bash read_null1362.sh <ins（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; TAG=${2:?タグ}; OUT=${3:?出力}; BASE=D:/PoB_work/keep_1355/base_ins
mkdir -p "$OUT"; cd "$R"
bash "$S/c78gz/stoplines906.sh" "$INS" "$OUT/stop906.txt" 1 > "$OUT/stop906.log" 2>&1; echo "stop906 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" 1 > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1362_20261125:m1362 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
echo READDONE
