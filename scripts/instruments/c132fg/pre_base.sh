#!/usr/bin/env bash
# (1283) c132fg 腕に依らない基準とヌル腕の側の読み（⑰ 同じ台本で数え直す・★(1254) 旗 0 の基準で止める線の台本が緑かの確かめ）：bash pre_base.sh <出力フォルダ（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
OUT=${1:?出力}; K=D:/PoB_work/keep_1280; BASE=$K/base_ins
mkdir -p "$OUT"; cd "$R"
bash "$S/c78gz/stoplines906.sh" "$BASE" "$OUT/stop906_base.txt" 1 > "$OUT/stop906_base.log" 2>&1; echo "stop906 base rc=$?"
bash "$S/c78gz/stoplines906.sh" "$K/n1" "$OUT/stop906_n1.txt" 1 > "$OUT/stop906_n1.log" 2>&1; echo "stop906 n1 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$K/n1" -v4 > "$OUT/hol_v4_n1.txt" 2>&1; echo "hol n1 rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$K/n1" > "$OUT/plvl_n1.txt" 2>&1; echo "plvl n1 rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" 1 > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$K/n1" 1 > "$OUT/ukdef_n1.txt" 2>&1; echo "ukdef n1 rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$BASE base=$BASE > "$OUT/gdp_zero10_base.txt" 2>&1; echo "gdpz base rc=$?"
python -X utf8 -I "$S/c132fg/magn_fg.py" "$BASE" "$BASE" 1 1 > "$OUT/magn_base.txt" 2>&1; echo "magn base rc=$?"
python -X utf8 -I "$S/c132fg/magn_fg.py" "$K/n1" "$BASE" 1 1 > "$OUT/magn_n1.txt" 2>&1; echo "magn n1 rc=$?"
echo PREDONE
