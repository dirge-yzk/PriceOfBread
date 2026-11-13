#!/usr/bin/env bash
# (1103) 合流の読み手（m1087/read1087.sh の写し・門を rr の gates_rr1103.py〔gates_rr.py の写し・種の範囲だけ〕と gp の gate1099.py〔変えずに〕へ・
#   記録に gp の反証役 v2 の最低賃金法の施行の行（mw1103.py）を足した・ヌル腕と NG の基準は m1087〔keep_1087/n1・n2・ng1087〕）
#   使い方: bash read1103.sh <ins のフォルダ（D:/ の形）> <成績表のファイル（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）> <種の起点> <基準の生（D:/ の形）> [門 0|1]
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; BASE=${6:?基準}; GATE=${7:-1}
SE=$((SB+9)); K=D:/PoB_work/keep_1087
mkdir -p "$OUT"
if [ "$GATE" = 1 ]; then
  python -X utf8 -I "$S/m1103/gates_rr1103.py" "$INS" "$BASE" $K/n1 $K/n2 $SB-$SE > "$OUT/gate_rr.txt" 2>&1; echo "gate_rr rc=$?"
  python -X utf8 -I "$S/c105gp/gate1099.py" "$OUT/gate_gp.txt" B=$INS:$SB-$SE base=$BASE:$SB-$SE n1=$K/n1:1-10 n2=$K/n2:1-10 > "$OUT/gate_gp.log" 2>&1; echo "gate_gp rc=$?"
fi
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261113_m1087_ins" "$R/measurements/b10_20261113_m1087_n1_ins" "$R/measurements/b10_20261113_m1087_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1087_20261113:m1087 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 -I "$S/m1103/mw1103.py" $SB-$SE "$INS" "$BASE" > "$OUT/rec_mw.txt" 2>&1; echo "rec_mw rc=$?"
[ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
echo READDONE
