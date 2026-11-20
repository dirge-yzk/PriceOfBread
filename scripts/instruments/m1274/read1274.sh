#!/usr/bin/env bash
# (1274) 合流の読み手（波130）：止める線と記録の共通の段は c130k15/read_k15.sh（popjump・stoplines1002・an_h2 -v4・gdp_zero10・plvl・ukdef・nullgate・.cost・home_break・ng・mag_k15〔k15 の H-a・H-b〕）を呼び、
#   rc の門（gate_rc.py＝H1〜H3）と記録（rec_disc.py・paper_rc.py）、gs の門（stoplines906・gs_sold.py＝H1・gs_read.py＝H2 の代わり）と記録（paper_gs.py・warcount.py）、
#   空洞化の件ごとの振り分け（m1247/holcase.py）・gdp_jump797・metal100・dec_after（read1267.sh と同じ）を足した。
#   基準は種 1〜10 が keep_1267/base_ins（INSRAW）・種 11〜20 が keep_1260/a11・ヌル腕は keep_1260/n1（KEEPNULL）。
#   使い方: bash read1274.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; SE=$((SB+9))
BASE=D:/PoB_work/keep_1267/base_ins; N1=D:/PoB_work/keep_1260/n1
[ "$SB" = 11 ] && BASE=D:/PoB_work/keep_1260/a11
mkdir -p "$OUT"
bash "$S/c130k15/read_k15.sh" "$INS" "$CARD" "$TAG" "$OUT" "$SB" "$BASE" "$N1" | grep -v READDONE
cd "$R"
# gs の止める線の台本（stoplines906）と門
bash "$S/c78gz/stoplines906.sh" "$INS" "$OUT/stop906.txt" "$SB" > "$OUT/stop906.log" 2>&1; echo "stop906 rc=$?"
bash "$S/c78gz/stoplines906.sh" "$BASE" "$OUT/stop906_base.txt" "$SB" > "$OUT/stop906_base.log" 2>&1; echo "stop906 base rc=$?"
python -X utf8 -I "$S/c130gs/gs_sold.py" "$R/measurements/ng1260_20261120/sold_m1260.txt" "$OUT/ng/sold_$TAG.txt" > "$OUT/H1_sold.txt" 2>&1; echo "gs H1 rc=$?"
python -X utf8 -I "$S/c130gs/gs_read.py" "$INS" "$BASE" $SB $SE > "$OUT/gs_read.txt" 2>&1; echo "gs_read rc=$?"
python -X utf8 -I "$S/c130gs/paper_gs.py" "$INS" $SB $SE > "$OUT/paper_gs.txt" 2>&1; echo "paper_gs rc=$?"
python -X utf8 -I "$S/c118nc/warcount.py" arm=$INS base=$BASE n1=$N1 > "$OUT/warcount.txt" 2>&1; echo "warcount rc=$?"
# rc の門と記録
python -X utf8 -I "$S/c130rc/gate_rc.py" "$INS" "$BASE" $SB $SE > "$OUT/gate_rc.txt" 2>&1; echo "gate_rc rc=$?"
python -X utf8 -I "$S/c130rc/paper_rc.py" "$INS" $SB $SE > "$OUT/paper_rc.txt" 2>&1; echo "paper_rc rc=$?"
python -X utf8 -I "$S/c130rc/rec_disc.py" "$INS" "$BASE" $SB $SE > "$OUT/rec_disc.txt" 2>&1; echo "rec_disc rc=$?"
# 空洞化の件ごとの振り分けと記録（read1267.sh と同じ）
python -X utf8 -I "$S/m1247/holcase.py" "$OUT/hol_v4.txt" "$BASE" "$N1" > "$OUT/holcase.txt" 2>&1; echo "holcase rc=$?"
python -X utf8 -I $S/m797/gdp_jump797.py arm=$INS base=$BASE n1=$N1 > "$OUT/gdpjump.txt" 2>&1; echo "gdpjump rc=$?"
python -X utf8 -I "$S/c127bd/metal100.py" "$INS" $SB $SE > "$OUT/metal.txt" 2>&1; echo "metal rc=$?"
python -X utf8 -I "$S/m1254/dec_after.py" "$INS" $SB $SE > "$OUT/dec_after.txt" 2>&1; echo "dec_after rc=$?"
echo READDONE
