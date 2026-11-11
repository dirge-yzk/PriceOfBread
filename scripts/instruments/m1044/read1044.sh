#!/usr/bin/env bash
# (1044) 合流の読み手（c98lw/read1037.sh の写し・種の起点と基準の生を引数にした・門は c98lw/gates_lw.py を変えずに）
# (1037) c98lw 腕の読み手（読むだけ・c96rd/read1025.sh の写し・基準を m1027 と keep_1035（INSRAW）・ヌル腕 keep_1027 へ・門を gates_lw.py へ・記録に record_lw.py）
#   使い方: bash read1044.sh <腕の作業フォルダ（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）> <種の起点> <基準の生（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
W=${1:?作業}; TAG=${2:?タグ}; OUT=${3:?出力}; SB=${4:?種の起点}; BASEARG=${5:?基準}
INS=$W/out_$TAG/ins; CARD=$W/b10_$TAG.txt; K=D:/PoB_work/keep_1027; BASE=$BASEARG
mkdir -p "$OUT"
python -X utf8 -I "$S/c98lw/gates_lw.py" "$INS" > "$OUT/gates_lw.txt" 2>&1; echo "gates rc=$?"
python -X utf8 -I "$S/c98lw/record_lw.py" "$BASE" "$INS" > "$OUT/record_lw.txt" 2>&1; echo "record rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261110_m1027_ins" "$R/measurements/b10_20261110_m1027_n1_ins" "$R/measurements/b10_20261110_m1027_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1027_20261110:m1027 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
echo READDONE