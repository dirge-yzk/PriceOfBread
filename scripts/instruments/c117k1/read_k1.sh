#!/usr/bin/env bash
# (1179) c117k1 腕 B の読み手（c116k1/read_k1.sh の写し・門は c117k1/gates_k1.py〔錨＝同じ木の旗 0＝keep_1178/base_ins〕・ヌル腕と NG の基準は m1178〔keep_1178/n1・n2・ng1178〕）
#   使い方: bash read_sv.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11> [門 0|1]
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; GATE=${6:-1}
SE=$((SB+9)); K=D:/PoB_work/keep_1178; BASE=$K/base_ins
mkdir -p "$OUT"
cd "$R"
if [ "$GATE" = 1 ]; then
  python -X utf8 -I "$S/c117k1/gates_k1.py" "$INS" "$SB" "$SE" "$OUT/gates_k1.txt" "$TAG" "${ANCH:-$BASE}" > "$OUT/gates_k1.log" 2>&1; echo "gates rc=$?"
  python -X utf8 -I "$S/c116k1/g3a_round.py" "$INS" "$SB" "$SE" > "$OUT/g3a_round.txt" 2>&1
  python -X utf8 -I "$S/c116k1/g1_split.py" "$INS" "$SB" "$SE" > "$OUT/g1_split.txt" 2>&1
fi
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261116_m1178_ins" "$R/measurements/b10_20261116_m1178_n1_ins" "$R/measurements/b10_20261116_m1178_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1178_20261116:m1178 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
echo READDONE
