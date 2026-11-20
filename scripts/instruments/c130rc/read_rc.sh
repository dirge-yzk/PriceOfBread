#!/usr/bin/env bash
# (1270) c130rc の読み手（c129al/read_al.sh の写し・al の台本〔gate_al2・discyr・rec_al〕を外し、gate_rc・swell・paper_rc を足した・基準の成績表 m1260・NG 基準 ng1260_20261120）。
#   使い方: bash read_rc.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11> <基線の生> <ヌル腕の生>
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
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261120_m1260_ins" "$R/measurements/b10_20261120_m1260_n1_ins" "$R/measurements/b10_20261120_m1260_n1_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
  diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$R/measurements/b10_20261120_m1260_ins") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$NUL --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1260_20261120:m1260 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 -I "$S/c130rc/gate_rc.py" "$INS" "$BASE" $SB $SE > "$OUT/gate_rc.txt" 2>&1; echo "gate_rc rc=$?"
python -X utf8 -I "$S/r1252/swell.py" "$TAG" "$INS" $SB $SE "$BASE" > "$OUT/swell.txt" 2>&1; echo "swell rc=$?"
python -X utf8 -I "$S/r1252/swell.py" base "$BASE" $SB $SE - > "$OUT/swell_base.txt" 2>&1; echo "swell base rc=$?"
python -X utf8 -I "$S/c130rc/paper_rc.py" "$INS" $SB $SE > "$OUT/paper_rc.txt" 2>&1; echo "paper_rc rc=$?"
echo READDONE
