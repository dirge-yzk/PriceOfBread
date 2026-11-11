#!/usr/bin/env bash
# (1025) c96rd 腕の読み手（読むだけ・c95ub/read1020.sh の写し・基準を m1022 と keep_1022 へ・門を gates1025 へ・記録に record1025 を足した）：
#   門 gates1025（事前登録 measurements/m1025_20261110_prereg_rd.txt [5]）・止める線（stoplines1002）・空洞化（an_h2 -v4）・
#   記録（N3 と越境 record1025・人口の比 popjump1017・NG・nullgate・gdp_zero10・plvl・ukdef・home_break・.cost）。
#   使い方: bash read1025.sh <腕の作業フォルダ（D:/ の形）> <タグ（out_<タグ>・b10_<タグ>.txt）> <種の起点 1|11> <出力フォルダ（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
W=${1:?作業}; TAG=${2:?タグ}; SB=${3:-1}; OUT=${4:?出力}
INS=$W/out_$TAG/ins; CARD=$W/b10_$TAG.txt; K=D:/PoB_work/keep_1022; ROWS=$R/measurements/m1025_20261110/rows.tsv
mkdir -p "$OUT"
if [ "$SB" = 11 ]; then
  python -X utf8 -I "$D0/gates1025.py" $ROWS $K/a11 "$INS" "$OUT/gates1025.tsv" > "$OUT/gates1025.txt" 2>&1; echo "gates rc=$?"
  python -X utf8 -I "$D0/record1025.py" $ROWS "$OUT/record1025.tsv" a11=$K/a11 S11=$INS > /dev/null 2>&1; echo "record rc=$?"
  python -X utf8 -I $S/m1017/popjump1017.py S11=$INS a11=$K/a11 --seeds=11-20 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
  BASE=$K/a11
else
  python -X utf8 -I "$D0/gates1025.py" $ROWS $K/base_ins "$INS" "$OUT/gates1025.tsv" > "$OUT/gates1025.txt" 2>&1; echo "gates rc=$?"
  python -X utf8 -I "$D0/record1025.py" $ROWS "$OUT/record1025.tsv" base=$K/base_ins B=$INS n1=$K/n1 n2=$K/n2 > /dev/null 2>&1; echo "record rc=$?"
  python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$K/base_ins N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
  BASE=$K/base_ins
fi
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$SB" = "1" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261110_m1022_ins" "$R/measurements/b10_20261110_m1022_n1_ins" "$R/measurements/b10_20261110_m1022_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
  bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1022_20261110:m1022 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
fi
grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
echo READDONE
