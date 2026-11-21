#!/usr/bin/env bash
# (1280) 合流の読み手（波131）：c131cr/read_cr.sh（止める線 stoplines1002・popjump・an_h2 -v4・gdp_zero10・plvl・ukdef・nullgate・.cost・home_break・ng・hol_attr・cr の門 gate_cr と記録 paper_cr）を呼び、
#   pf の止める線の台本（c78gz/stoplines906・腕と基準）と門（c131pf/magn_pf3.py＝H1・H6）と記録（capskip_sum.py・xborder.py）、
#   空洞化の件ごとの振り分け（m1247/holcase.py）・gdp_jump797・metal100・dec_after（read1274.sh と同じ）を足した。
#   基準は種 1〜10 が keep_1274/base_ins（INSRAW）・種 11〜20 が keep_1274/a11・ヌル腕は keep_1274/n1（KEEPNULL）。成績表の差は種 1〜10 が b10_20261121_m1274_ins・種 11〜20 が b10s11_20261121_m1274_ins と数える。
#   使い方: bash read1280.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; SE=$((SB+9))
BASE=D:/PoB_work/keep_1274/base_ins; N1=D:/PoB_work/keep_1274/n1; CB=$R/measurements/b10_20261121_m1274_ins
[ "$SB" = 11 ] && BASE=D:/PoB_work/keep_1274/a11 && CB=$R/measurements/b10s11_20261121_m1274_ins
mkdir -p "$OUT"
bash "$S/c131cr/read_cr.sh" "$INS" "$CARD" "$TAG" "$OUT" "$SB" "$BASE" "$N1" new | grep -v READDONE
cd "$R"
if [ "$CARD" != "-" ]; then
  diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$CB") | grep -c '^[<>]' > "$OUT/card_difflines_samebase.txt"
fi
# pf の止める線の台本と門・記録
bash "$S/c78gz/stoplines906.sh" "$INS" "$OUT/stop906.txt" "$SB" > "$OUT/stop906.log" 2>&1; echo "stop906 rc=$?"
bash "$S/c78gz/stoplines906.sh" "$BASE" "$OUT/stop906_base.txt" "$SB" > "$OUT/stop906_base.log" 2>&1; echo "stop906 base rc=$?"
python -X utf8 -I "$S/c131pf/magn_pf3.py" "$INS" "$BASE" $SB $SB > "$OUT/magn_pf.txt" 2>&1; echo "magn_pf rc=$?"
python -X utf8 -I "$S/c131pf/capskip_sum.py" "$INS" $SB $SE > "$OUT/capskip.txt" 2>&1; echo "capskip rc=$?"
python -X utf8 -I "$S/c131pf/xborder.py" "$INS" "$BASE" $SB $SB > "$OUT/xborder.txt" 2>&1; echo "xborder rc=$?"
# 空洞化の件ごとの振り分けと記録（read1274.sh と同じ）
python -X utf8 -I "$S/m1247/holcase.py" "$OUT/hol_v4.txt" "$BASE" "$N1" > "$OUT/holcase.txt" 2>&1; echo "holcase rc=$?"
python -X utf8 -I $S/m797/gdp_jump797.py arm=$INS base=$BASE n1=$N1 > "$OUT/gdpjump.txt" 2>&1; echo "gdpjump rc=$?"
python -X utf8 -I "$S/c127bd/metal100.py" "$INS" $SB $SE > "$OUT/metal.txt" 2>&1; echo "metal rc=$?"
python -X utf8 -I "$S/m1254/dec_after.py" "$INS" $SB $SE > "$OUT/dec_after.txt" 2>&1; echo "dec_after rc=$?"
echo READDONE
