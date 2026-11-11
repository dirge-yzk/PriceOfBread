#!/usr/bin/env bash
# (1020) c95ub 腕の読み手（読むだけ・c94pe/read1014.sh の写し・基準を m1017 と keep_1017 へ・門を gates1020 へ）：
#   門 gates1020（事前登録 measurements/m1020_20261110_prereg_ub.txt [5]）・止める線（stoplines1002）・空洞化（an_h2 -v4）・
#   記録（人口の比 popjump1017・NG・nullgate・gdp_zero10・plvl・ukdef・home_break・甜菜糖 rh_beet・.cost）。
#   使い方: bash read1020.sh <腕の作業フォルダ（D:/ の形）> <タグ（out_<タグ>・b10_<タグ>.txt）> <種の起点 1|11> <出力フォルダ（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
W=${1:?作業}; TAG=${2:?タグ}; SB=${3:-1}; OUT=${4:?出力}
INS=$W/out_$TAG/ins; CARD=$W/b10_$TAG.txt; K=D:/PoB_work/keep_1017
mkdir -p "$OUT"
if [ "$SB" = 11 ]; then
  python -X utf8 -I "$D0/gates1020.py" gates "$INS" $K/a11 $K/a11 $K/a11 11-20 "$OUT/gates1020.txt" > /dev/null 2>&1; echo "gates rc=$?"
  python -X utf8 -I $S/m1017/popjump1017.py S11=$INS a11=$K/a11 --seeds=11-20 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
  BASE=$K/a11
else
  python -X utf8 -I "$D0/gates1020.py" gates "$INS" $K/base_ins $K/n1 $K/n2 1-10 "$OUT/gates1020.txt" > /dev/null 2>&1; echo "gates rc=$?"
  python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$K/base_ins N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
  BASE=$K/base_ins
fi
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$SB" = "1" ]; then
  python -X utf8 -I $S/r1015/rh_beet.py $R/PriceOfBread.SimCore/world.json "$INS" 1-10 "$OUT/beet.tsv" > /dev/null 2>&1; echo "beet rc=$?"
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261110_m1017_ins" "$R/measurements/b10_20261110_m1017_n1_ins" "$R/measurements/b10_20261110_m1017_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
  bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1017_20261110:m1017 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
fi
grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
echo READDONE
