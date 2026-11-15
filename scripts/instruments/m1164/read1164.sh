#!/usr/bin/env bash
# (1164) 合流の読み手（m1157/read1157.sh の型・門を c114qn の c113qn/gates.py・c114qn/expread.py〔変えずに〕へ・ヌル腕と NG の基準は m1157〔keep_1157/n1・n2・ng1157〕）
#   使い方: bash read1164.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11> <基準の生（D:/ の形）> <門 0|1> [旗 0 の側の colqn_s の生（D:/ の形）]
#   門 1＝qn の門 G1・G2・G4（G4 の床＝旗 0 の側の fuelClaim＞0 の行の 0.5 倍＝gates.py の --flag0）。門 0＝止める線と記録だけ。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; BASE=${6:?基準}; GATE=${7:-1}; OFF=${8:--}
SE=$((SB+9)); K=D:/PoB_work/keep_1157
mkdir -p "$OUT"
cd "$R"
if [ "$GATE" = 1 ]; then
  python -X utf8 -I "$S/c113qn/gates.py" . "$INS" --flag0 "$OFF" > "$OUT/gates_qn.txt" 2>&1; echo "gates_qn rc=$?"
  python -X utf8 -I "$S/c114qn/expread.py" arm="$INS" flag0="$OFF" > "$OUT/exp.txt" 2>&1; echo "exp rc=$?"
  python -X utf8 -I "$S/c113qn/periph.py" . B="$INS" > "$OUT/periph.txt" 2>&1; echo "periph rc=$?"
fi
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261115_m1157_ins" "$R/measurements/b10_20261115_m1157_n1_ins" "$R/measurements/b10_20261115_m1157_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1157_20261115:m1157 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
echo READDONE
