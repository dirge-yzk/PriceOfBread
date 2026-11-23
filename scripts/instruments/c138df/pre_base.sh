#!/usr/bin/env bash
# (1324) c138df 腕に依らない基準とヌル腕の側の読み（紙の台本・⑰ 同じ台本で数え直す・★(1254) 旗 0 の基準で止める線の台本が緑かの確かめ・c137ak/pre_base.sh と c132df/read_df.sh の写し）。
#   基準＝INSRAW keep_1321/base_ins（旗 0＝m1321）・ヌル腕＝KEEPNULL keep_1321/n1・n2・a11（種 11〜20）。
#   bash pre_base.sh <出力フォルダ（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
OUT=${1:?出力}; K=D:/PoB_work/keep_1321; BASE=$K/base_ins
mkdir -p "$OUT"; cd "$R"
python -X utf8 -I $S/c132df/gate_df.py "$BASE" 1 10 base > "$OUT/gate_base.txt" 2>&1; echo "gate base rc=$?"
python -X utf8 -I $S/c132df/gate_df.py "$K/n1" 1 10 n1 > "$OUT/gate_n1.txt" 2>&1; echo "gate n1 rc=$?"
python -X utf8 -I $S/c132df/gate_df.py "$K/n2" 1 10 n2 > "$OUT/gate_n2.txt" 2>&1; echo "gate n2 rc=$?"
python -X utf8 -I $S/c132df/gate_df.py "$K/a11" 11 20 a11 > "$OUT/gate_a11.txt" 2>&1; echo "gate a11 rc=$?"
python -X utf8 -I "$S/c132df/arrears_sum.py" "$BASE" 1 10 > "$OUT/arrears_base.txt" 2>&1; echo "arrears base rc=$?"
bash "$S/c78gz/stoplines906.sh" "$BASE" "$OUT/stop906_base.txt" 1 > "$OUT/stop906_base.log" 2>&1; echo "stop906 base rc=$?"
bash "$S/c78gz/stoplines906.sh" "$K/n1" "$OUT/stop906_n1.txt" 1 > "$OUT/stop906_n1.log" 2>&1; echo "stop906 n1 rc=$?"
bash "$S/c78gz/stoplines906.sh" "$K/n2" "$OUT/stop906_n2.txt" 1 > "$OUT/stop906_n2.log" 2>&1; echo "stop906 n2 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$K/n1" > "$OUT/plvl_n1.txt" 2>&1; echo "plvl n1 rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" 1 > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$K/n1" 1 > "$OUT/ukdef_n1.txt" 2>&1; echo "ukdef n1 rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$K/n2" 1 > "$OUT/ukdef_n2.txt" 2>&1; echo "ukdef n2 rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$BASE base=$BASE > "$OUT/gdp_zero10_base.txt" 2>&1; echo "gdpz base rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$BASE base=$BASE N1=$K/n1 > "$OUT/popjump_base.txt" 2>&1; echo "popjump base rc=$?"
python -X utf8 -I "$S/r1313/q12_born.py" "$OUT/q12_M" 1-10 M=$BASE > "$OUT/q12_M.log" 2>&1; echo "q12 M rc=$?"
python -X utf8 -I "$S/r1313/q12_born.py" "$OUT/q12_N1" 1-10 N1=$K/n1 > "$OUT/q12_N1.log" 2>&1; echo "q12 N1 rc=$?"
python -X utf8 -I "$S/r1313/q12_born.py" "$OUT/q12_N2" 1-10 N2=$K/n2 > "$OUT/q12_N2.log" 2>&1; echo "q12 N2 rc=$?"
echo PREDONE
