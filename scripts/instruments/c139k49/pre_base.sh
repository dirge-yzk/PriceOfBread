#!/usr/bin/env bash
# (1331) c139k49（c137k49 の写し）腕に依らない基準とヌル腕の側の読み（⑰ 同じ台本で数え直す・旗 0 の基準で止める線の台本が緑かの確かめ・c134k49/pre_base.sh の写し）。
#   紙の台本＝基準 INSRAW keep_1328/base_ins（m1328 と全行一致の腕 Z の生）を腕と基準の両方として当てる（何もしない実装の盤面で門が落ちないことの確かめ）＋ヌル腕 KEEPNULL keep_1328/n1 を腕として当てる（記録）。
#   keep_1328/n1 には prodcap_s が無い＝史実の桁の読み手 mag_c139 は N1 に当てない（記録の欄が空）。
#   bash pre_base.sh <出力フォルダ（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
OUT=${1:?出力}; BASE=D:/PoB_work/keep_1328/base_ins; N1=D:/PoB_work/keep_1328/n1
mkdir -p "$OUT"; cd "$R"
bash "$S/c78gz/stoplines906.sh" "$BASE" "$OUT/stop906_base.txt" 1 > "$OUT/stop906_base.log" 2>&1; echo "stop906 base rc=$?"
bash "$S/c78gz/stoplines906.sh" "$N1" "$OUT/stop906_n1.txt" 1 > "$OUT/stop906_n1.log" 2>&1; echo "stop906 n1 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$N1" -v4 > "$OUT/hol_v4_n1.txt" 2>&1; echo "hol n1 rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$N1" > "$OUT/plvl_n1.txt" 2>&1; echo "plvl n1 rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" 1 > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$N1" 1 > "$OUT/ukdef_n1.txt" 2>&1; echo "ukdef n1 rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$BASE base=$BASE > "$OUT/gdp_zero10_base.txt" 2>&1; echo "gdpz base rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$BASE base=$BASE N1=$N1 > "$OUT/popjump_base.txt" 2>&1; echo "popjump base rc=$?"
python -X utf8 -I "$S/c139k49/mag_c139.py" "$BASE" "$BASE" 1 10 > "$OUT/mag_base.txt" 2>&1; echo "mag base rc=$?"
python -X utf8 -I "$S/c139k49/ukcrash_c139.py" "$BASE" 1 10 > "$OUT/ukcrash_base.txt" 2>&1; echo "ukcrash base rc=$?"
python -X utf8 -I "$S/c139k49/ukcrash_c139.py" "$N1" 1 10 > "$OUT/ukcrash_n1.txt" 2>&1; echo "ukcrash n1 rc=$?"
python -X utf8 -I "$S/c139k49/k49_perm.py" "$BASE" 1 10 > "$OUT/perm_base.txt" 2>&1; echo "perm base rc=$?"   # (1331) 記録 H の誤りの外れの率（単独でも回した）
echo PREDONE
