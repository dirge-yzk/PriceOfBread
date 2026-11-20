#!/usr/bin/env bash
# (1267) 合流の読み手：c129al/read_al.sh（al の門 H1・H2・H3・G-B1・止める線 stoplines1002・空洞化 an_h2 -v4・NG・nullgate・ukdef・home_break・gdp_zero10）を呼び、
#   空洞化の件ごとの振り分け（m1247/holcase.py）・gdp_jump797・metal100（(1262) の直し＝1 四半期以内の組だけ）・dec_after を足した。
#   基準は keep_1260（read1260.sh の keep_1254 から付け替えた＝(1262) の申し送り）。S11 の基準は keep_1260/a11。ヌル腕は keep_1260/n1。
#   使い方: bash read1267.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; SE=$((SB+9))
K=D:/PoB_work/keep_1260; BASE=$K/base_ins; N1=$K/n1
[ "$SB" = 11 ] && BASE=$K/a11
mkdir -p "$OUT"
bash "$S/c129al/read_al.sh" "$INS" "$CARD" "$TAG" "$OUT" "$SB" "$BASE" "$N1" | grep -v READDONE
cd "$R"
python -X utf8 -I "$S/m1247/holcase.py" "$OUT/hol_v4.txt" "$BASE" "$N1" > "$OUT/holcase.txt" 2>&1; echo "holcase rc=$?"
python -X utf8 -I $S/m797/gdp_jump797.py arm=$INS base=$BASE n1=$N1 > "$OUT/gdpjump.txt" 2>&1; echo "gdpjump rc=$?"
python -X utf8 -I "$S/c127bd/metal100.py" "$INS" $SB $SE > "$OUT/metal.txt" 2>&1; echo "metal rc=$?"
python -X utf8 -I "$S/c127bd/metal100.py" "$BASE" $SB $SE > "$OUT/metal_base.txt" 2>&1; echo "metal base rc=$?"
python -X utf8 -I "$S/m1254/dec_after.py" "$INS" $SB $SE > "$OUT/dec_after.txt" 2>&1; echo "dec_after rc=$?"
echo READDONE
