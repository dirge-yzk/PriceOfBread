#!/usr/bin/env bash
# (1030) c97eb 腕の読み手（読むだけ・c96rd/read1025.sh の写し・基準を m1027 と keep_1027 へ・門を m1030.py＋gates1030.py＋mc1030.py へ）：
#   門（事前登録 measurements/m1030_20261111_prereg_eb.txt [3]・追記 1）・止める線（stoplines1002）・空洞化（an_h2 -v4）・
#   記録（人口の比 popjump1017・NG・nullgate・gdp_zero10・plvl・ukdef・home_break・.cost）。
#   使い方: bash read1030.sh <腕の作業フォルダ（D:/ の形）> <タグ（out_<タグ>・b10_<タグ>.txt）> <出力フォルダ（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
W=${1:?作業}; TAG=${2:?タグ}; OUT=${3:?出力}; SB=1
INS=$W/out_$TAG/ins; CARD=$W/b10_$TAG.txt; K=D:/PoB_work/keep_1027; WJ=$R/PriceOfBread.SimCore/world.json; GJ=$R/PriceOfBread.SimCore/goods.json
MET=D:/PoB_work/c97eb_work
mkdir -p "$OUT"
python -X utf8 -I "$D0/m1030.py" "$INS" 1-10 $WJ $GJ "$OUT/met_arm.tsv" > "$OUT/met.log" 2>&1; echo "met rc=$?"
python -X utf8 -I "$D0/gates1030.py" "$OUT/met_arm.tsv" $MET/met_base_ins.tsv $MET/met_n1.tsv $MET/met_n2.tsv armB > "$OUT/gates1030.txt" 2>&1; echo "gates rc=$?"
python -X utf8 -I "$D0/mc1030.py" "$INS" $K/base_ins 1-10 > "$OUT/mc1030.txt" 2>&1; echo "mc rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$K/base_ins N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
BASE=$K/base_ins
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261110_m1027_ins" "$R/measurements/b10_20261110_m1027_n1_ins" "$R/measurements/b10_20261110_m1027_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1027_20261110:m1027 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 -I "$S/c96rd/costcmp1025.py" "$R/measurements/b10_20261110_m1027_ins.cost" "$CARD.cost" 14 > "$OUT/costcmp.txt" 2>&1; echo "cost rc=$?"
echo READDONE
