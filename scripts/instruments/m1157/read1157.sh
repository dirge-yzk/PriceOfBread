#!/usr/bin/env bash
# (1157) 合流（m1150/read1150.sh の写し・門を c113lc の gates_lc.py・gc0.py〔変えずに〕へ・種 11〜20 の G-C1a の床は旗 0 の側〔S11z〕の値の 10 倍・ヌル腕と NG の基準は m1150〔keep_1150/n1・n2・ng1150〕）
# (1150) 合流の読み手（m1143/read1143.sh の型・門を c112hy の gates.py〔変えずに〕と g4s11.py〔種 11〜20 の G4〕へ・ヌル腕と NG の基準は m1143〔keep_1143/n1・n2・ng1143〕）
#   使い方: bash read1150.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11> <基準の生（D:/ の形）> <門 0|1> [旗 0 の側の ins（D:/ の形）]
#   門 1＝hy の門を読む（種の起点 1＝gates.py の G1〜G4・種の起点 11＝gates.py の G1〜G3＋g4s11.py の G4）。門 0＝止める線と記録だけ。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; BASE=${6:?基準}; GATE=${7:-1}; OFF=${8:--}
SE=$((SB+9)); K=D:/PoB_work/keep_1150
mkdir -p "$OUT"
cd "$R"
if [ "$GATE" = 1 ]; then
  # (1157) lc の門（scripts/instruments/c113lc の gates_lc.py・gc0.py を変えずに）。OFF＝旗 0 の側の ins（種 11〜20 の G-C1a の床を 10×その値で取る＝★(1150) の型）。
  FLOOR=4772
  if [ "$SB" != 1 ] && [ "$OFF" != "-" ]; then python -X utf8 -I "$S/c113lc/gates_lc.py" "$OFF" $SB $SE > "$OUT/gates_lc_flag0.txt" 2>&1; V0=$(grep -oP "G-C1a 知る国から買った機関車の額 10 種の和 \K[0-9.e+]+" "$OUT/gates_lc_flag0.txt"); FLOOR=$(python -c "print(10*float('$V0'))"); echo "flag0 G-C1a $V0 floor $FLOOR"; fi
  python -X utf8 -I "$S/c113lc/gates_lc.py" "$INS" $SB $SE $FLOOR > "$OUT/gates_lc.txt" 2>&1; echo "gates_lc rc=$?"
  python -X utf8 -I "$S/c113lc/gc0.py" "$BASE" "$INS" $SB $SE > "$OUT/gc0.txt" 2>&1; echo "gc0 rc=$?"
  python -X utf8 -I $S/r1149/rail_hist.py "$INS" $SB $SE > "$OUT/rail.txt" 2>&1; echo "rail rc=$?"
  python -X utf8 -I $S/r1149/loco_import.py "$INS" $SB $SE > "$OUT/loco.txt" 2>&1; echo "loco rc=$?"
fi
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261115_m1150_ins" "$R/measurements/b10_20261115_m1150_n1_ins" "$R/measurements/b10_20261115_m1150_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1150_20261115:m1150 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
echo READDONE
