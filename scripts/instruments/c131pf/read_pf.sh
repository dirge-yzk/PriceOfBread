#!/usr/bin/env bash
# (1277) c131pf 腕 B の読み手（c126pf/read_pf.sh の写し・基線の生は keep_1274/base_ins・ヌル腕は keep_1274/n1・NG の基準は ng1274）。
#   基準とヌル腕の側の読みは pre_base.sh（腕に依らない・同じ台本）。
#   門＝止める線（stoplines906・plvl_seeds・popjump1017・an_h2 -v4・.cost）＋史実の桁の線 H1・H6（magn_pf3.py）。他は記録。
#   使い方: bash read_pf.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; SE=$((SB+9))
K=D:/PoB_work/keep_1274; BASE=D:/PoB_work/keep_1274/base_ins
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c78gz/stoplines906.sh" "$INS" "$OUT/stop906.txt" "$SB" > "$OUT/stop906.log" 2>&1; echo "stop906 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261121_m1274_ins" "$R/measurements/b10_20261121_m1274_n1_ins" "$R/measurements/b10_20261121_m1274_n1_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
  diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$R/measurements/b10_20261121_m1274_ins") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1274_20261121:m1274 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 -I "$S/c131pf/magn_pf3.py" "$INS" "$BASE" $SB 1 > "$OUT/magn.txt" 2>&1; echo "magn rc=$?"
python -X utf8 -I "$S/c131pf/capskip_sum.py" "$INS" $SB $SE > "$OUT/capskip.txt" 2>&1; echo "capskip rc=$?"
python -X utf8 -I "$S/c131pf/capskip_sum.py" "D:/PoB_work/ident_c131pf/out_ib_10/ins" 1 10 > "$OUT/capskip_z.txt" 2>&1; echo "capskip z rc=$?"
python -X utf8 -I "$S/c131pf/xborder.py" "$INS" "$BASE" $SB 1 > "$OUT/xborder.txt" 2>&1; echo "xborder rc=$?"
echo READDONE
