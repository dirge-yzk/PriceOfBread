#!/usr/bin/env bash
# (1290) c133mr 腕 B の読み手（c132fg/read_fg.sh の写し・基線の生は keep_1287/base_ins・ヌル腕は keep_1287/n1・NG の基準は ng1287_20261122）。
#   基準とヌル腕の側の読みは pre_base.sh（腕に依らない・同じ台本）。
#   門＝止める線（stoplines906・plvl_seeds・popjump1017・an_h2 -v4・.cost）。史実の桁の線と記録＝lt_sum（一番動かす量）・q3_paper（H1）・rec_mr（sameCore・統一・民族の緊張）。
#   使い方: bash read_mr.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}
K=D:/PoB_work/keep_1287; BASE=$K/base_ins
WJ=$R/PriceOfBread.SimCore/world.json; LB=$S/c131cr/colony_labels.tsv
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c78gz/stoplines906.sh" "$INS" "$OUT/stop906.txt" "$SB" > "$OUT/stop906.log" 2>&1; echo "stop906 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261122_m1287_ins" "$R/measurements/b10_20261122_m1287_n1_ins" "$R/measurements/b10_20261122_m1287_n1_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
  diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$R/measurements/b10_20261122_m1287_ins") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1287_20261122:m1287 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 -I "$S/c133mr/lt_sum.py" "$INS" $SB 10 > "$OUT/lt_sum.txt" 2>&1; echo "lt rc=$?"
python -X utf8 -I "$S/r1285/q3_paper.py" "$INS" $SB 10 "$WJ" "$LB" > "$OUT/q3_paper_arm.txt" 2>&1; echo "q3 rc=$?"
python -X utf8 -I "$S/c133mr/rec_mr.py" "$INS" $SB 10 arm > "$OUT/rec_arm.txt" 2>&1; echo "rec rc=$?"
echo READDONE
