#!/usr/bin/env bash
# (1298) c134k49 腕に依らない基準とヌル腕の側の読み（⑰ 同じ台本で数え直す・★(1254) 旗 0 の基準で止める線の台本が緑かの確かめ・c133mr/pre_base.sh の写し）。
#   紙の台本＝基準 keep_1294/base_ins を腕と基準の両方として当てる（旗 0 の盤面で門が落ちないことの確かめ）＋ヌル腕 n1 を腕として当てる（記録）。
#   bash pre_base.sh <出力フォルダ（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
OUT=${1:?出力}; K=D:/PoB_work/keep_1294; BASE=$K/base_ins
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
python -X utf8 -I "$S/c134k49/mag_k49.py" "$BASE" "$BASE" 1 10 > "$OUT/mag_base.txt" 2>&1; echo "mag base rc=$?"
python -X utf8 -I "$S/c134k49/mag_k49.py" "$K/n1" "$BASE" 1 10 > "$OUT/mag_n1.txt" 2>&1; echo "mag n1 rc=$?"
python -X utf8 -I "$S/c134k49/rec_k49.py" "$BASE" 1 10 base > "$OUT/rec_base.txt" 2>&1; echo "rec base rc=$?"
python -X utf8 -I "$S/c134k49/rec_k49.py" "$K/n1" 1 10 n1 > "$OUT/rec_n1.txt" 2>&1; echo "rec n1 rc=$?"
echo PREDONE
