#!/usr/bin/env bash
# (1254) 合流の読み手（m1247/read1247.sh・c127nr/read_nr.sh・c127bd/read_bd.sh を 1 本に合わせた写し）。
#   基線の生：種 1〜10＝keep_1247/base_ins（m1233 の世界・全表）・種 11〜20＝keep_1233/a11・ヌル腕＝keep_1233/n1・NG の基準 ng1233_20261119。
#   門＝止める線（stoplines1002・plvl_seeds・popjump1017・an_h2 -v4・gdp_zero10・.cost）＋nr の史実の桁（Hu1・Hu2・Hw＝unif_read・H1・H2＝paper・H3＝magn）
#        ＋bd の史実の桁 H1（ukprice）と機構の観測量 G1（read_bd.py）。他は記録。
#   使い方: bash read1254.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; SE=$((SB+9))
K=D:/PoB_work/keep_1233; BASE=D:/PoB_work/keep_1247/base_ins; N1=$K/n1
[ "$SB" = 11 ] && BASE=$K/a11
mkdir -p "$OUT"
cd "$R"
# 止める線
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$BASE" "$OUT/stop1002_base.txt" "$SB" > "$OUT/stop1002_base.log" 2>&1; echo "stop1002 base rc=$?"
[ "$SB" = 1 ] && { bash "$S/c92gf/stoplines1002.sh" "$N1" "$OUT/stop1002_n1.txt" 1 > "$OUT/stop1002_n1.log" 2>&1; echo "stop1002 n1 rc=$?"; }
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$N1" > "$OUT/plvl_n1.txt" 2>&1; echo "plvl n1 rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$N1 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I $S/m797/gdp_jump797.py arm=$INS base=$BASE n1=$N1 > "$OUT/gdpjump.txt" 2>&1; echo "gdpjump rc=$?"
if [ "$CARD" != "-" ]; then
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
  B10=$R/measurements/b10_20261119_m1233_ins; [ "$SB" = 11 ] && B10=$R/measurements/b10s11_20261119_m1233_ins
  diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$B10") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261103_m797_ins" "$R/measurements/b10_20261103_m797_n1_ins" "$R/measurements/b10_20261103_m797_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
fi
# nr の門（史実の桁）と記録
python -X utf8 -I "$S/c124nr/unif_read.py" "$BASE" "$INS" > "$OUT/unif_read.txt" 2>&1; echo "unif_read rc=$?"
python -X utf8 -I "$S/c119nd/paper.py" "$BASE" $SB $SE "$INS" > "$OUT/paper_B.txt" 2>&1; echo "paper rc=$?"
python -X utf8 -I "$S/c119nd/paper.py" "$INS" $SB $SE > "$OUT/paper_Bself.txt" 2>&1; echo "paper self rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1233_20261119:m1233 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 -I "$S/c118nc/magn.py" "$R/measurements/ng1233_20261119/mil180_m1233.txt" "$OUT/ng/mil180_$TAG.txt" > "$OUT/magn_H.txt" 2>&1; echo "magn rc=$?"
python -X utf8 -I "$S/c118nc/warcount.py" M=$BASE N1=$N1 B=$INS > "$OUT/warcount.txt" 2>&1; echo "warcount rc=$?"
python -X utf8 -I "$S/c124nr/w1.py" "$BASE" "$INS" > "$OUT/w1.txt" 2>&1; echo "w1 rc=$?"
python -X utf8 -I "$S/c124nr/formations.py" M=$BASE N1=$N1 B=$INS > "$OUT/formations.txt" 2>&1; echo "formations rc=$?"
python -X utf8 -I $S/r1225/declare_first.py "$INS" $SB $SE > "$OUT/declare_first.txt" 2>&1; echo "declare_first rc=$?"
LS="M=$BASE N1=$N1 B=$INS"; [ "$SB" = 11 ] && LS="M=$BASE B=$INS"
for t in $LS; do n=${t%%=*}; d=${t#*=}; python -X utf8 -I "$S/c127nr/h3uk.py" "$d" $SB $SE $n; done > "$OUT/h3uk.txt" 2>&1; echo "h3uk rc=$?"
for t in $LS; do n=${t%%=*}; d=${t#*=}; python -X utf8 -I "$S/r1238/uk_seedtab.py" "$d" $SB $SE $n; done > "$OUT/uk_seedtab.txt" 2>&1; echo "uk_seedtab rc=$?"
# bd の門（史実の桁 H1・機構の観測量 G1）と記録
python -X utf8 -I "$S/c127bd/ukprice.py" "$INS" $SB $SE > "$OUT/bd_h1.txt" 2>&1; echo "bd h1 rc=$?"
python -X utf8 -I "$S/c127bd/ukprice.py" "$BASE" $SB $SE > "$OUT/bd_h1_base.txt" 2>&1; echo "bd h1 base rc=$?"
python -X utf8 -I "$S/c127bd/read_bd.py" "$INS" $SB $SE > "$OUT/g1.txt" 2>&1; echo "g1 rc=$?"
python -X utf8 -I "$S/c127bd/read_bd.py" "$BASE" $SB $SE > "$OUT/g1_base.txt" 2>&1; echo "g1 base rc=$?"
python -X utf8 -I "$S/c127bd/metal100.py" "$INS" $SB $SE > "$OUT/metal.txt" 2>&1; echo "metal rc=$?"
[ "$SB" = 1 ] && { python -X utf8 -I "$S/c127bd/alldef.py" arm=$INS base=$BASE n1=$N1 > "$OUT/alldef.txt" 2>&1; echo "alldef rc=$?"; }
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" "$SB" > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$N1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
echo READDONE
