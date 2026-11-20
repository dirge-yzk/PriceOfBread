#!/usr/bin/env bash
# (1271) c130gs の読み手（c128uf/read_uf.sh の写しを基準 m1260〔生 keep_1267/base_ins〕・N1 keep_1260/n1・NG 基準 ng1260・成績表 m1260 へ・gs の門に絞った）。
#   門＝止める線（stoplines906・plvl_seeds・popjump1017・an_h2 -v4・.cost）＋史実の桁（H1＝gs_sold.py・H2＝gs_read.py ②）。他は記録。
#   使い方: bash read_gs.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; SE=$((SB+9))
BASE=D:/PoB_work/keep_1267/base_ins; N1=D:/PoB_work/keep_1260/n1
mkdir -p "$OUT"
cd "$R"
# 止める線
bash "$S/c78gz/stoplines906.sh" "$INS" "$OUT/stop906.txt" "$SB" > "$OUT/stop906.log" 2>&1; echo "stop906 rc=$?"
bash "$S/c78gz/stoplines906.sh" "$BASE" "$OUT/stop906_base.txt" "$SB" > "$OUT/stop906_base.log" 2>&1; echo "stop906 base rc=$?"
bash "$S/c78gz/stoplines906.sh" "$N1" "$OUT/stop906_n1.txt" 1 > "$OUT/stop906_n1.log" 2>&1; echo "stop906 n1 rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$N1" > "$OUT/plvl_n1.txt" 2>&1; echo "plvl n1 rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$N1 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
if [ "$CARD" != "-" ]; then
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
  B10=$R/measurements/b10_20261120_m1260_ins
  diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$B10") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261120_m1260_ins" "$R/measurements/b10_20261120_m1260_n1_ins" "$R/measurements/b10_20261120_m1260_n1_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
fi
# 史実の桁（H1・H2）
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1260_20261120:m1260 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 -I "$D0/gs_sold.py" "$R/measurements/ng1260_20261120/sold_m1260.txt" "$OUT/ng/sold_$TAG.txt" > "$OUT/H1_sold.txt" 2>&1; echo "H1 rc=$?"
python -X utf8 -I "$D0/gs_read.py" "$INS" "$BASE" $SB $SE > "$OUT/gs_read.txt" 2>&1; echo "gs_read rc=$?"
# 記録
python -X utf8 -I "$D0/paper_gs.py" "$INS" $SB $SE > "$OUT/paper_gs.txt" 2>&1; echo "paper_gs rc=$?"
python -X utf8 -I "$S/c118nc/warcount.py" arm=$INS base=$BASE n1=$N1 > "$OUT/warcount.txt" 2>&1; echo "warcount rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$N1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
echo READDONE
