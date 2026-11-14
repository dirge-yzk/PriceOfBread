#!/usr/bin/env bash
# (1136) 合流の読み手（m1122/read1122.sh の写し・門を lo の gates_lo.py〔変えずに〕へ・ヌル腕と NG の基準は m1122〔keep_1122/n1・n2・ng1122〕・成績表の基準は m1122・記録に lo の disc_count.py〔種 1〜10 だけ〕）
#   使い方: bash read1136.sh <ins のフォルダ（D:/ の形）> <成績表のファイル（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）> <種の起点> <基準の生（D:/ の形）> <旗 0 の railspend_s の生（D:/ の形）か -> [門 0|1]
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; BASE=${6:?基準}; G0=${7:?旗0}; GATE=${8:-1}
SE=$((SB+9)); K=D:/PoB_work/keep_1122
SEEDS=$(seq -s, $SB $SE)
mkdir -p "$OUT"
if [ "$GATE" = 1 ]; then
  python -X utf8 -I "$S/c110lo/gates_lo.py" "$INS" "$G0" "$SEEDS" > "$OUT/gates_lo.txt" 2>&1; echo "gates_lo rc=$?"
fi
if [ "$SB" = 1 ] && [ "$G0" != - ]; then python -X utf8 -I "$S/c110lo/disc_count.py" "$INS" "$G0" "$BASE" "$K/n1" "$K/n2" > "$OUT/disc_count.txt" 2>&1; echo "disc rc=$?"; fi
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261114_m1122_ins" "$R/measurements/b10_20261114_m1122_n1_ins" "$R/measurements/b10_20261114_m1122_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1122_20261114:m1122 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
[ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
echo READDONE
