#!/usr/bin/env bash
# (1087) 合流の読み手（m1069/read1069.sh の写し・門を cr の gate1082.py〔変えずに〕と dm の gate_dm1087.py〔予言 追記 1 で腕の前に固定〕へ・記録は cr の rec1082.py と dm の rec_dm.py・rec_treas.py・ヌル腕と NG の基準は m1069）
#   使い方: bash read1087.sh <腕の作業フォルダ（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）> <種の起点> <基準の生（D:/ の形）> [門 0|1]
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
W=${1:?作業}; TAG=${2:?タグ}; OUT=${3:?出力}; SB=${4:?種の起点}; BASE=${5:?基準}; GATE=${6:-1}
INS=$W/out_$TAG/ins; CARD=$W/b10_$TAG.txt; K=D:/PoB_work/keep_1069
mkdir -p "$OUT"
if [ "$GATE" = 1 ]; then
  python -X utf8 -I "$S/c103cr/gate1082.py" "$INS" $SB 10 > "$OUT/gate_cr.txt" 2>&1; echo "gate_cr rc=$?"
  python -X utf8 -I "$S/m1087/gate_dm1087.py" "$INS" --seeds $SB-$((SB+9)) --flowseeds $SB-$((SB+2)) --tree $R > "$OUT/gate_dm.txt" 2>&1; echo "gate_dm rc=$?"
fi
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261112_m1069_ins" "$R/measurements/b10_20261112_m1069_n1_ins" "$R/measurements/b10_20261112_m1069_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1069_20261112:m1069 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 -I "$S/c103cr/rec1082.py" B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/rec_cr.txt" 2>&1; echo "rec_cr rc=$?"
python -X utf8 -I "$R/measurements/m1081_20261112/rec_dm.py" B=$INS M=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/rec_dm.txt" 2>&1; echo "rec_dm rc=$?"
python -X utf8 -I "$R/measurements/m1081_20261112/rec_treas.py" B=$INS M=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/rec_treas.txt" 2>&1; echo "rec_treas rc=$?"
grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
echo READDONE
