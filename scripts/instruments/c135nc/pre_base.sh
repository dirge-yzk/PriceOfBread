#!/usr/bin/env bash
# (1304) c135nc 腕に依らない基準とヌル腕の側の読み（⑰ 同じ台本で数え直す・★(1254) 旗 0 の基準で止める線の台本が緑かの確かめ・c134k49/pre_base.sh の写し）。
#   基準＝INSRAW keep_1301/base_ins・ヌル腕＝KEEPNULL keep_1294/n1・紙の台本 paper_nc.py を M・S11（keep_1294/a11）・N1 に当てる（旗 0 で G1 が落ちることの確かめ）。
#   bash pre_base.sh <出力フォルダ（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
OUT=${1:?出力}; K=D:/PoB_work/keep_1294; BASE=D:/PoB_work/keep_1301/base_ins
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
python -X utf8 -I $S/m1017/popjump1017.py B=$BASE base=$BASE N1=$K/n1 > "$OUT/popjump_base.txt" 2>&1; echo "popjump base rc=$?"
python -X utf8 -I "$S/c135nc/paper_nc.py" "$BASE" 1 10 > "$OUT/paper_M.txt" 2>&1; echo "paper M rc=$?"
python -X utf8 -I "$S/c135nc/paper_nc.py" "$K/a11" 11 10 > "$OUT/paper_S11.txt" 2>&1; echo "paper S11 rc=$?"
python -X utf8 -I "$S/c135nc/paper_nc.py" "$K/n1" 1 10 > "$OUT/paper_N1.txt" 2>&1; echo "paper N1 rc=$?"
echo PREDONE
