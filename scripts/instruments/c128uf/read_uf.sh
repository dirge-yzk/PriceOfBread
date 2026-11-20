#!/usr/bin/env bash
# (1258) c128uf の読み手（m1254/read1254.sh の写しを keep_1254 の基線・ヌル腕 N1・NG の基準 ng1254 へ・uf の門だけに絞った）。
#   門＝止める線（stoplines906・plvl_seeds・popjump1017・an_h2 -v4・.cost）＋史実の桁（Hu1・Hu2・Hw＝unif_read_s・H1・H2＝paper・H3＝magn）＋機構の観測量 G1（uf_read.py の UA）。他は記録。
#   使い方: bash read_uf.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; SE=$((SB+9))
K=D:/PoB_work/keep_1254; BASE=$K/base_ins; N1=$K/n1
[ "$SB" = 11 ] && BASE=$K/a11
mkdir -p "$OUT"
cd "$R"
# 止める線
bash "$S/c78gz/stoplines906.sh" "$INS" "$OUT/stop906.txt" "$SB" > "$OUT/stop906.log" 2>&1; echo "stop906 rc=$?"
bash "$S/c78gz/stoplines906.sh" "$BASE" "$OUT/stop906_base.txt" "$SB" > "$OUT/stop906_base.log" 2>&1; echo "stop906 base rc=$?"
[ "$SB" = 1 ] && { bash "$S/c78gz/stoplines906.sh" "$N1" "$OUT/stop906_n1.txt" 1 > "$OUT/stop906_n1.log" 2>&1; echo "stop906 n1 rc=$?"; }
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$N1" > "$OUT/plvl_n1.txt" 2>&1; echo "plvl n1 rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$N1 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
if [ "$CARD" != "-" ]; then
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
  B10=$R/measurements/b10_20261120_m1254_ins; [ "$SB" = 11 ] && B10=$R/measurements/b10s11_20261120_m1254_ins
  diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$B10") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261120_m1254_ins" "$R/measurements/b10_20261120_m1254_n1_ins" "$R/measurements/b10_20261120_m1254_n1_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
fi
# 史実の桁（Hu1・Hu2・Hw・H1・H2・H3）
python -X utf8 -I "$S/m1254/unif_read_s.py" "$BASE" "$INS" $SB > "$OUT/unif_read.txt" 2>&1; echo "unif_read rc=$?"
python -X utf8 -I "$S/c119nd/paper.py" "$BASE" $SB $SE "$INS" > "$OUT/paper_B.txt" 2>&1; echo "paper rc=$?"
python -X utf8 -I "$S/c119nd/paper.py" "$INS" $SB $SE > "$OUT/paper_Bself.txt" 2>&1; echo "paper self rc=$?"   # H1・H2 の腕の値（腕 B の後に足した＝宣言した H1・H2 の読みの欠け・数え方は同じ）
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1254_20261120:m1254 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
MAGNB="$R/measurements/ng1254_20261120/mil180_m1254.txt"; [ "$SB" = 11 ] && MAGNB="$R/measurements/m1254_20261120/readS11/ng/mil180_mS11.txt"   # S11 の H3 は S11 の基線の ng と比べる（(1260) の取り直しの形）
python -X utf8 -I "$S/c118nc/magn.py" "$MAGNB" "$OUT/ng/mil180_$TAG.txt" > "$OUT/magn_H.txt" 2>&1; echo "magn rc=$?"
# 機構の観測量 G1（UA）と記録
python -X utf8 -I "$D0/uf_read.py" "$INS" $SB $SE B > "$OUT/uf_B.txt" 2>&1; echo "uf B rc=$?"
python -X utf8 -I "$D0/uf_read.py" "$BASE" $SB $SE M > "$OUT/uf_M.txt" 2>&1; echo "uf M rc=$?"
python -X utf8 -I "$S/m1254/dec_after.py" "$INS" $SB $SE > "$OUT/dec_after.txt" 2>&1; echo "dec_after rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
[ "$SB" = 1 ] && { python -X utf8 -I "$S/c127bd/alldef.py" arm=$INS base=$BASE n1=$N1 > "$OUT/alldef.txt" 2>&1; echo "alldef rc=$?"; }
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$N1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
echo READDONE
