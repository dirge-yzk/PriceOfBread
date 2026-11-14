#!/usr/bin/env bash
# (1143) 合流の読み手（m1122/read1122.sh の型・門を rb の gates_rb.py・tp の count_tport.py・lo の gates_lo.py〔どれも変えずに〕へ・ヌル腕と NG の基準は m1122〔keep_1122/n1・n2・ng1122〕）
#   使い方: bash read1143.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11> <基準の生（D:/ の形）> <門 0|1> [lo の旗 0 の側の ins（D:/ の形）]
#   門 1＝rb・tp・lo の門を読む（lo の旗 0 の側を渡さなければ lo は読まない）。門 0＝止める線と記録だけ。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; BASE=${6:?基準}; GATE=${7:-1}; LO0=${8:-}
SE=$((SB+9)); K=D:/PoB_work/keep_1122; SEEDS=$(seq -s, $SB $SE)
mkdir -p "$OUT"
cd "$R"
if [ "$GATE" = 1 ]; then
  python -X utf8 -I "$S/c111rb/gates_rb.py" "$INS" "$TAG" "$OUT/gates_rb.txt" > /dev/null 2>&1; echo "gates_rb rc=$?"
  python -X utf8 -I "$S/c111tp/count_tport.py" "$INS" 1 10 $SB > "$OUT/gates_tp.txt" 2>&1; echo "gates_tp rc=$?"
  if [ -n "$LO0" ]; then python -X utf8 -I "$S/c110lo/gates_lo.py" "$INS" "$LO0" $SEEDS > "$OUT/gates_lo.txt" 2>&1; echo "gates_lo rc=$?"; fi
fi
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261114_m1122_ins" "$R/measurements/b10_20261114_m1122_n1_ins" "$R/measurements/b10_20261114_m1122_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1122_20261114:m1122 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
echo READDONE
