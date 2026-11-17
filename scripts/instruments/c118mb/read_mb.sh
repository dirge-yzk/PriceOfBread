#!/usr/bin/env bash
# (1187) c118mb 腕 B の読み手（c116hd/read_hd.sh の型・基準とヌル腕と NG は m1184〔keep_1184/base_ins・n1・n2・ng1184〕・門は c118mb/mb.py）
#   使い方: bash read_mb.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}
K=D:/PoB_work/keep_1184; BASE=$K/base_ins; P=$R/measurements/m1187_20261117/paper
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I "$S/c118mb/mb.py" paper "$INS" 1 10 "$OUT/paper_B.json" > "$OUT/paper_B.txt" 2>&1; echo "paper rc=$?"
python -X utf8 -I "$S/c118mb/mb.py" gates "$OUT/paper_B.json" $P/M.json $P/N1.json $P/N2.json > "$OUT/gates_mb.txt" 2>&1; echo "gates rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" 1 > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" 1 > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261116_m1184_ins" "$R/measurements/b10_20261116_m1184_n1_ins" "$R/measurements/b10_20261116_m1184_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1184_20261116:m1184 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
echo READDONE
