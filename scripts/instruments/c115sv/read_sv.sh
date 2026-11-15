#!/usr/bin/env bash
# (1166) c115sv 腕 B の読み手（m1164/read1164.sh の型・門は c115sv/gates_sv.py・ヌル腕と NG の基準は m1164〔keep_1164/n1・n2・ng1164〕）
#   使い方: bash read_sv.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11> [門 0|1]
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; GATE=${6:-1}
SE=$((SB+9)); K=D:/PoB_work/keep_1164; BASE=$K/base_ins
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I "$S/c115sv/paper_sv.py" "$INS" "$SB" "$SE" "$OUT/paper.txt" > "$OUT/paper.log" 2>&1; echo "paper rc=$?"
if [ "$GATE" = 1 ]; then
  python -X utf8 -I "$S/c115sv/gates_sv.py" "$INS" "$OUT/paper.txt" "$SB" "$SE" > "$OUT/gates_sv.txt" 2>&1; echo "gates rc=$?"
fi
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261115_m1164_ins" "$R/measurements/b10_20261115_m1164_n1_ins" "$R/measurements/b10_20261115_m1164_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1164_20261115:m1164 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
echo READDONE
