#!/usr/bin/env bash
# (1250) c127bd 腕 B の読み手（c127bd/read_bd.sh の写し・基線は m1233〔INSRAW keep_1247/base_ins〕・ヌル腕 keep_1233/n1・NG の基準 ng1233_20261119）。
#   門＝止める線（stoplines1002・plvl_seeds・popjump1017・metal100・an_h2 -v4・.cost）＋史実の桁 H1（ukprice）＋機構の観測量 G1（read_bd.py）。他は記録。
#   使い方: bash read_bd.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; SE=$((SB+9))
BASE=D:/PoB_work/keep_1247/base_ins; N1=D:/PoB_work/keep_1233/n1
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I "$S/c127bd/read_bd.py" "$INS" $SB $SE > "$OUT/g1.txt" 2>&1; echo "g1 rc=$?"
python -X utf8 -I "$S/c127bd/read_bd.py" "$BASE" 1 10 > "$OUT/g1_base.txt" 2>&1; echo "g1 base rc=$?"
python -X utf8 -I "$S/c127bd/read_bd.py" "$N1" 1 10 > "$OUT/g1_n1.txt" 2>&1; echo "g1 n1 rc=$?"
python -X utf8 -I "$S/c127bd/ukprice.py" "$INS" $SB $SE > "$OUT/h1.txt" 2>&1; echo "h1 rc=$?"
python -X utf8 -I "$S/c127bd/metal100.py" "$INS" $SB $SE > "$OUT/metal.txt" 2>&1; echo "metal rc=$?"
python -X utf8 -I "$S/c127bd/metal100.py" "$N1" 1 10 > "$OUT/metal_n1.txt" 2>&1; echo "metal n1 rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$N1 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$BASE" "$OUT/stop1002_base.txt" "$SB" > "$OUT/stop1002_base.log" 2>&1; echo "stop1002 base rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$N1" > "$OUT/plvl_n1.txt" 2>&1; echo "plvl n1 rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" "$SB" > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261119_m1233_ins" "$R/measurements/b10_20261119_m1233_n1_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
  diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$R/measurements/b10_20261119_m1233_ins") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$N1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1233_20261119:m1233 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 -I $S/m797/gdp_jump797.py arm=$INS base=$BASE n1=$N1 > "$OUT/gdpjump.txt" 2>&1; echo "gdpjump rc=$?"
python -X utf8 -I "$S/c127bd/shadow_bd.py" "$INS" $SB $SE armB > "$OUT/shadow_armB.tsv" 2>&1; echo "shadow rc=$?"
echo READDONE
