#!/usr/bin/env bash
# (1283) c132fg の読み手（c131cr/read_cr.sh の写し・cr の台本〔gate_cr・paper_cr〕を外し gate_fg を足した・基準の成績表 m1280・NG 基準 ng1280_20261121・nullgate の N1 は m1280_n1）。
#   使い方: bash read_fg.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11> <基線の生> <ヌル腕の生>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; SE=$((SB+9))
BASE=${6:?基線の生}; NUL=${7:?ヌル腕の生}
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I "$S/c123k3/histpop_k3.py" "$INS" $SB $SE > "$OUT/h1.txt" 2>&1; echo "h1 rc=$?"
python -X utf8 -I "$S/c123k3/histpop_k3.py" "$BASE" $SB $SE > "$OUT/h1_base.txt" 2>&1; echo "h1 base rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$NUL > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$BASE" "$OUT/stop1002_base.txt" "$SB" > "$OUT/stop1002_base.log" 2>&1; echo "stop1002 base rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$NUL" "$OUT/stop1002_n1.txt" 1 > "$OUT/stop1002_n1.log" 2>&1; echo "stop1002 n1 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$NUL" > "$OUT/plvl_n1.txt" 2>&1; echo "plvl n1 rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" "$SB" > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261121_m1280_ins" "$R/measurements/b10_20261121_m1280_n1_ins" "$R/measurements/b10_20261121_m1280_n1_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
  diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$R/measurements/b10_20261121_m1280_ins") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$NUL --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1280_20261121:m1280 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v5 > "$OUT/hol_v5.txt" 2>&1; echo "hol v5 rc=$?"
python -X utf8 -I "$S/c131cr/hol_attr.py" "$OUT/hol_v5.txt" "$BASE" > "$OUT/hol_attr.txt" 2>&1; echo "hol_attr rc=$?"
python -X utf8 -I "$S/c132fg/gate_fg.py" "$INS" "$BASE" $SB > "$OUT/gate_fg.txt" 2>&1; echo "gate_fg rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$NUL base=$BASE > "$OUT/gdp_zero10_n1.txt" 2>&1; echo "gdpz n1 rc=$?"
echo READDONE
