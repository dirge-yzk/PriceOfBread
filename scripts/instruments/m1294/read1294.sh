#!/usr/bin/env bash
# (1294) 合流の読み手（波133・m1287/read1287.sh の写し）：止める線と記録の並びは (1287) と同じ（基準の成績表と NG を m1287・ng1287 へ）。df の門（gate_df）を外し、
#   mr の門と一番動かす量（c133mr/lt_sum.py・r1285/q3_paper.py・c133mr/rec_mr.py）を足した。fg の史実の桁の線（c132fg/magn_fg.py）と記録（arrears_sum.py）は残す。
#   空洞化の件ごとの振り分け（m1247/holcase.py）・gdp_jump797・metal100・dec_after も (1287) と同じ。
#   基準は種 1〜10 が keep_1287/base_ins（INSRAW）・種 11〜20 が keep_1287/a11・ヌル腕は keep_1287/n1（KEEPNULL）。
#   成績表の差は種 1〜10 が b10_20261122_m1287_ins・種 11〜20 が b10s11_20261122_m1287_ins と数える。
#   使い方: bash read1294.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; SE=$((SB+9))
K=D:/PoB_work/keep_1287; BASE=$K/base_ins; NUL=$K/n1; CB=$R/measurements/b10_20261122_m1287_ins
[ "$SB" = 11 ] && BASE=$K/a11 && CB=$R/measurements/b10s11_20261122_m1287_ins
mkdir -p "$OUT"
cd "$R"
# 止める線（基準とヌル腕も同じ台本で＝⑰）
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$NUL > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$BASE" "$OUT/stop1002_base.txt" "$SB" > "$OUT/stop1002_base.log" 2>&1; echo "stop1002 base rc=$?"
bash "$S/c78gz/stoplines906.sh" "$INS" "$OUT/stop906.txt" "$SB" > "$OUT/stop906.log" 2>&1; echo "stop906 rc=$?"
bash "$S/c78gz/stoplines906.sh" "$BASE" "$OUT/stop906_base.txt" "$SB" > "$OUT/stop906_base.log" 2>&1; echo "stop906 base rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m1247/holcase.py" "$OUT/hol_v4.txt" "$BASE" "$NUL" > "$OUT/holcase.txt" 2>&1; echo "holcase rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
python -X utf8 -I $S/m797/gdp_jump797.py arm=$INS base=$BASE n1=$NUL > "$OUT/gdpjump.txt" 2>&1; echo "gdpjump rc=$?"
# 史実の桁の線（線が一番動かす量）
python -X utf8 -I "$S/c132fg/magn_fg.py" "$INS" "$BASE" $SB $SB > "$OUT/magn_fg.txt" 2>&1; echo "magn_fg rc=$?"
# (1294) mr の門と一番動かす量（c133mr/read_mr.sh と同じ台本）
python -X utf8 -I "$S/c133mr/lt_sum.py" "$INS" $SB 10 > "$OUT/lt_sum.txt" 2>&1; echo "lt rc=$?"
python -X utf8 -I "$S/r1285/q3_paper.py" "$INS" $SB 10 "$R/PriceOfBread.SimCore/world.json" "$S/c131cr/colony_labels.tsv" > "$OUT/q3_paper_arm.txt" 2>&1; echo "q3 rc=$?"
python -X utf8 -I "$S/c133mr/rec_mr.py" "$INS" $SB 10 arm > "$OUT/rec_arm.txt" 2>&1; echo "rec rc=$?"
# 記録
python -X utf8 -I "$S/c132df/arrears_sum.py" "$INS" $SB $SE > "$OUT/arrears.txt" 2>&1; echo "arrears rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" "$SB" > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$NUL --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
python -X utf8 -I "$S/c127bd/metal100.py" "$INS" $SB $SE > "$OUT/metal.txt" 2>&1; echo "metal rc=$?"
python -X utf8 -I "$S/m1254/dec_after.py" "$INS" $SB $SE > "$OUT/dec_after.txt" 2>&1; echo "dec_after rc=$?"
python -X utf8 -I "$S/c131cr/gate_cr.py" "$INS" new "$BASE" old "$R/PriceOfBread.SimCore/world.json" "$S/c131cr/colony_labels.tsv" $SB > "$OUT/gate_cr.txt" 2>&1; echo "gate_cr rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261122_m1287_ins" "$R/measurements/b10_20261122_m1287_n1_ins" "$R/measurements/b10_20261122_m1287_n1_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
  diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$CB") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
fi
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1287_20261122:m1287 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
echo READDONE
