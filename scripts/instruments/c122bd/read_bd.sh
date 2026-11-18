#!/usr/bin/env bash
# (1215) c122bd 腕 B の読み手（c119nd/read_nd.sh の写し・基線とヌル腕と NG の基準は m1212〔keep_1212/base_ins・n1〕・ng1198）。
#   門＝止める線（stoplines1002・plvl_seeds・popjump1017・metal100・an_h2 -v4・.cost）＋史実の桁 H1（ukprice）＋機構の観測量 G1（read_bd.py）。他は記録。
#   使い方: bash read_bd.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; SE=$((SB+9))
K=D:/PoB_work/keep_1212; BASE=$K/base_ins
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I "$S/c122bd/read_bd.py" "$INS" $SB $SE > "$OUT/g1.txt" 2>&1; echo "g1 rc=$?"
python -X utf8 -I "$S/c122bd/read_bd.py" "$BASE" 1 10 > "$OUT/g1_base.txt" 2>&1; echo "g1 base rc=$?"
python -X utf8 -I "$S/c122bd/read_bd.py" "$K/n1" 1 10 > "$OUT/g1_n1.txt" 2>&1; echo "g1 n1 rc=$?"
python -X utf8 -I "$S/c122bd/ukprice.py" "$INS" $SB $SE > "$OUT/h1.txt" 2>&1; echo "h1 rc=$?"
python -X utf8 -I "$S/c122bd/metal100.py" "$INS" $SB $SE > "$OUT/metal.txt" 2>&1; echo "metal rc=$?"
python -X utf8 -I "$S/c122bd/metal100.py" "$K/n1" 1 10 > "$OUT/metal_n1.txt" 2>&1; echo "metal n1 rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$BASE" "$OUT/stop1002_base.txt" "$SB" > "$OUT/stop1002_base.log" 2>&1; echo "stop1002 base rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$K/n1" > "$OUT/plvl_n1.txt" 2>&1; echo "plvl n1 rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" "$SB" > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261118_m1212_ins" "$R/measurements/b10_20261118_m1212_n1_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
  diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$R/measurements/b10_20261118_m1212_ins") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1198_20261118:m1198 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
echo READDONE
