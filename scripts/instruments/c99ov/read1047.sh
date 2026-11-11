#!/usr/bin/env bash
# (1047) c99ov 腕の読み手（読むだけ・c98lw/read1037.sh の写し・基準を m1044 と keep_1044（INSRAW・ヌル腕 n1・n2）へ・門を gates_ov.py へ・記録に record_ov.py）
#   使い方: bash read1037.sh <腕の作業フォルダ（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）>（種 1〜10 だけ）
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
W=${1:?作業}; TAG=${2:?タグ}; OUT=${3:?出力}; SB=1
INS=$W/out_$TAG/ins; CARD=$W/b10_$TAG.txt; K=D:/PoB_work/keep_1044; BASE=D:/PoB_work/keep_1044/base_ins
mkdir -p "$OUT"
python -X utf8 -I "$D0/gates_ov.py" "$OUT/gates_ov.txt" B=$INS base=$BASE n1=$K/n1 n2=$K/n2 > /dev/null 2>&1; echo "gates rc=$?"
python -X utf8 -I "$D0/record_ov.py" "$OUT/record_ov.txt" B=$INS base=$BASE n1=$K/n1 n2=$K/n2 > /dev/null 2>&1; echo "record rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261111_m1044_ins" "$R/measurements/b10_20261111_m1044_n1_ins" "$R/measurements/b10_20261111_m1044_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1044_20261111:m1044 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
echo READDONE