#!/usr/bin/env bash
# (1188) c118ur 腕 B の読み手（c116hd/read_hd.sh の型・門は c118ur/ur.py・ヌル腕と NG の基準は m1184〔keep_1184/n1・n2・ng1184〕）
#   使い方: bash read_hd.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11> [門 0|1]
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; GATE=${6:-1}
SE=$((SB+9)); K=D:/PoB_work/keep_1184; BASE=$K/base_ins; P=$R/measurements/m1188_20261117
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I "$S/c118ur/ur.py" paper "$INS" "$SB" "$SE" "$OUT/paper_B.json" > "$OUT/paper_B.txt" 2>&1; echo "paper rc=$?"
if [ "$GATE" = 1 ]; then
  python -X utf8 -I "$S/c118ur/ur.py" gates "$OUT/paper_B.json" $P/paper_M.json > "$OUT/gates_ur.txt" 2>&1; echo "gates rc=$?"
fi
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261116_m1184_ins" "$R/measurements/b10_20261116_m1184_n1_ins" "$R/measurements/b10_20261116_m1184_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1184_20261116:m1184 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
echo READDONE
