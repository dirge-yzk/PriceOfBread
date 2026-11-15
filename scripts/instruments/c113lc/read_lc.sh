#!/usr/bin/env bash
# (1152) c113lc 腕 B の読み手（c111tp/read_tp.sh の型・基準は INSRAW keep_1150/base_ins とヌル腕 keep_1150/n1・n2・成績表 m1150・NG ng1150）
#   使い方: bash read_lc.sh <ins のフォルダ（D:/ の形）> <成績表のファイル（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）>
#   止める線の 100 倍と 1e10 倍の基準とヌル腕は measurements/m1150_20261115/stopbase/（同じ台本 stoplines1002 の出力）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}
K=D:/PoB_work/keep_1150; BASE=$K/base_ins
mkdir -p "$OUT"
python -X utf8 -I "$D0/gates_lc.py" "$INS" 1 10 4772 > "$OUT/gates_lc.txt" 2>&1; echo "gates rc=$?"
python -X utf8 -I "$D0/gc0.py" "$BASE" "$INS" 1 10 > "$OUT/gc0.txt" 2>&1; echo "gc0 rc=$?"
python -X utf8 -I "$D0/paper_lc.py" "$INS" 1 10 > "$OUT/paper_B.txt" 2>&1; echo "paper rc=$?"
python -X utf8 -I $S/r1149/known_count.py "$INS" 1 10 > "$OUT/known_B.txt" 2>&1; echo "known rc=$?"
python -X utf8 -I $S/r1149/rail_hist.py "$INS" 1 10 > "$OUT/rail_B.txt" 2>&1; echo "rail rc=$?"
python -X utf8 -I $S/r1149/rail_hist.py "$BASE" 1 10 > "$OUT/rail_M.txt" 2>&1; echo "railM rc=$?"
python -X utf8 -I $S/r1149/loco_import.py "$INS" 1 10 > "$OUT/loco_B.txt" 2>&1; echo "loco rc=$?"
python -X utf8 -I $S/c111rb/gates_rb.py "$INS" B "$OUT/gates_rb_B.txt" > /dev/null 2>&1; echo "rb rc=$?"
python -X utf8 -I $S/c111rb/gates_rb.py "$BASE" M "$OUT/gates_rb_M.txt" > /dev/null 2>&1; echo "rbM rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" 1 > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" 1 > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261115_m1150_ins" "$R/measurements/b10_20261115_m1150_n1_ins" "$R/measurements/b10_20261115_m1150_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1150_20261115:m1150 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
[ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
echo READDONE
