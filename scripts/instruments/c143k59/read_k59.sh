#!/usr/bin/env bash
# (1356) c143k59 腕 B の読み手（m1355/read_m1355.sh の写しから、この線の門と記録の分だけ）。基線の生は INSRAW keep_1355/base_ins・ヌル腕は keep_1355/n1・n2・NG の基準は ng1355_20261125。
#   基準とヌル腕の側は (1355) の合流が同じ台本で数えた measurements/m1355_20261125/readM・readN1・readN2（⑰）。
#   使い方: bash read_k59.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}
K=D:/PoB_work/keep_1355; BASE=$K/base_ins; BC=b10_20261125_m1355_ins
[ "$SB" = 11 ] && { BASE=$K/a11; BC=b10s11_20261125_m1355_ins; }
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I "$D0/femp_k59.py" "$INS" > "$OUT/femp.txt" 2>&1; echo "femp rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c78gz/stoplines906.sh" "$INS" "$OUT/stop906.txt" "$SB" > "$OUT/stop906.log" 2>&1; echo "stop906 rc=$?"
bash "$S/c139sl/stoplines1330.sh" "$INS" "$OUT/stop1330.txt" "$SB" > "$OUT/stop1330.log" 2>&1; echo "stop1330 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/$BC" "$R/measurements/b10_20261125_m1355_n1_ins" "$R/measurements/b10_20261125_m1355_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
  diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$R/measurements/$BC") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1355_20261125:m1355 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 -I "$S/c142lr/bham.py" "$INS" > "$OUT/bham.txt" 2>&1; echo "bham rc=$?"
python -X utf8 -I "$S/c142lr/bham.py" "$INS" セントポール > "$OUT/stpaul.txt" 2>&1; echo "stpaul rc=$?"
[ -f "$INS/lwk_s$SB.tsv" ] && { python -X utf8 -I "$D0/lwk_k59.py" "$INS" > "$OUT/lwk.txt" 2>&1; echo "lwk rc=$?"; }
echo READDONE
