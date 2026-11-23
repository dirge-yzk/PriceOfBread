#!/usr/bin/env bash
# (1318) c137ak 腕に依らない基準とヌル腕の側の読み（⑰ 同じ台本で数え直す・★(1254) 旗 0 の基準で止める線の台本が緑かの確かめ・c135nc/pre_base.sh の写し）。
#   基準＝INSRAW keep_1315/base_ins・ヌル腕＝KEEPNULL keep_1308/n1・n2・a11（種 11〜20）。
#   紙＝r1313/q4_share.py（分与の新÷旧の近似）と r1313/q12_born.py（生まれの民間÷国庫 1/1000 未満・1 人あたり名目 GDP 1/20 未満）を M・S11・N1・N2 に当てる。
#   bash pre_base.sh <出力フォルダ（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
OUT=${1:?出力}; K=D:/PoB_work/keep_1308; BASE=D:/PoB_work/keep_1315/base_ins
mkdir -p "$OUT"; cd "$R"
bash "$S/c78gz/stoplines906.sh" "$BASE" "$OUT/stop906_base.txt" 1 > "$OUT/stop906_base.log" 2>&1; echo "stop906 base rc=$?"
bash "$S/c78gz/stoplines906.sh" "$K/n1" "$OUT/stop906_n1.txt" 1 > "$OUT/stop906_n1.log" 2>&1; echo "stop906 n1 rc=$?"
bash "$S/c78gz/stoplines906.sh" "$K/n2" "$OUT/stop906_n2.txt" 1 > "$OUT/stop906_n2.log" 2>&1; echo "stop906 n2 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$K/n1" -v4 > "$OUT/hol_v4_n1.txt" 2>&1; echo "hol n1 rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$K/n1" > "$OUT/plvl_n1.txt" 2>&1; echo "plvl n1 rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" 1 > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$K/n1" 1 > "$OUT/ukdef_n1.txt" 2>&1; echo "ukdef n1 rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$BASE base=$BASE > "$OUT/gdp_zero10_base.txt" 2>&1; echo "gdpz base rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$BASE base=$BASE N1=$K/n1 > "$OUT/popjump_base.txt" 2>&1; echo "popjump base rc=$?"
python -X utf8 -I "$S/r1313/q4_share.py" "$OUT/q4_share_M.tsv" 1-10 M=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/q4_share_M.log" 2>&1; echo "q4 M rc=$?"
python -X utf8 -I "$S/r1313/q4_share.py" "$OUT/q4_share_S11.tsv" 11-20 S11=$K/a11 > "$OUT/q4_share_S11.log" 2>&1; echo "q4 S11 rc=$?"
python -X utf8 -I "$S/r1313/q12_born.py" "$OUT/q12_M" 1-10 M=$BASE > "$OUT/q12_M.log" 2>&1; echo "q12 M rc=$?"
python -X utf8 -I "$S/r1313/q12_born.py" "$OUT/q12_N1" 1-10 N1=$K/n1 > "$OUT/q12_N1.log" 2>&1; echo "q12 N1 rc=$?"
python -X utf8 -I "$S/r1313/q12_born.py" "$OUT/q12_N2" 1-10 N2=$K/n2 > "$OUT/q12_N2.log" 2>&1; echo "q12 N2 rc=$?"
python -X utf8 -I "$S/r1313/q12_born.py" "$OUT/q12_S11" 11-20 S11=$K/a11 > "$OUT/q12_S11.log" 2>&1; echo "q12 S11 rc=$?"
echo PREDONE
