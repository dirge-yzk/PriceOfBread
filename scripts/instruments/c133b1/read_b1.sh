#!/usr/bin/env bash
# (1291) c133b1 腕 B の読み手（c132df/read_df.sh の写し・基線の生は keep_1287/base_ins・ヌル腕は keep_1287/n1・NG の基準は ng1287）。
#   使い方: bash read_df.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; SE=$((SB+9))
K=D:/PoB_work/keep_1287; BASE=$K/base_ins
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c78gz/stoplines906.sh" "$INS" "$OUT/stop906.txt" "$SB" > "$OUT/stop906.log" 2>&1; echo "stop906 rc=$?"
bash "$S/c78gz/stoplines906.sh" "$K/n1" "$OUT/stop906_n1.txt" 1 > "$OUT/stop906_n1.log" 2>&1; echo "stop906 n1 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$K/n1" > "$OUT/plvl_n1.txt" 2>&1; echo "plvl n1 rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261122_m1287_ins" "$R/measurements/b10_20261122_m1287_n1_ins" "$R/measurements/b10_20261122_m1287_n1_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
  diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$R/measurements/b10_20261122_m1287_ins") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1287_20261122:m1287 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 -I "$S/c132df/arrears_sum.py" "$INS" $SB $SE > "$OUT/arrears.txt" 2>&1; echo "arrears rc=$?"
python -X utf8 -I "$S/r998/q3_trade_gdp.py" "$INS" "$TAG" "$OUT/h1.txt" $SB-$SE > "$OUT/h1.log" 2>&1; echo "h1 rc=$?"
python -X utf8 -I "$S/r1197/an8.py" "$INS" $SB $SE "$OUT/h2.txt" > "$OUT/h2.log" 2>&1; echo "h2 rc=$?"
python -X utf8 -I "$S/c121b1/b1read.py" "$INS" $SB $SE "$BASE" > "$OUT/b1read.txt" 2>&1; echo "b1read rc=$?"
echo READDONE
