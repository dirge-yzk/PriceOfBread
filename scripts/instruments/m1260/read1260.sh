#!/usr/bin/env bash
# (1260) 合流の読み手：c128uf/read_uf.sh（keep_1254 の基線・N1・ng1254・uf の門）をそのまま呼び、記録の読み手を足した
#   （stoplines1002〔(1254) の止める線の台本〕・gdp_jump797・ukdef の基準・metal100〔1 四半期以内の 2 行だけ比べる＝(1262)・silverst_s は疎な表〕・dec_after の基準）。
#   使い方: bash read1260.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; SE=$((SB+9))
K=D:/PoB_work/keep_1315; BASE=$K/base_ins; N1=D:/PoB_work/keep_1308/n1   # (1316) keep_1254 は消えた＝基線の生は keep_1315（INSRAW）・ヌル腕 N1 は keep_1308（KEEPNULL）。種 11〜20（$K/a11）は keep_1308/a11
[ "$SB" = 11 ] && BASE=D:/PoB_work/keep_1308/a11
mkdir -p "$OUT"
bash "$S/c128uf/read_uf.sh" "$INS" "$CARD" "$TAG" "$OUT" "$SB" | grep -v READDONE
cd "$R"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$BASE" "$OUT/stop1002_base.txt" "$SB" > "$OUT/stop1002_base.log" 2>&1; echo "stop1002 base rc=$?"
[ "$SB" = 1 ] && { bash "$S/c92gf/stoplines1002.sh" "$N1" "$OUT/stop1002_n1.txt" 1 > "$OUT/stop1002_n1.log" 2>&1; echo "stop1002 n1 rc=$?"; }
python -X utf8 -I $S/m797/gdp_jump797.py arm=$INS base=$BASE n1=$N1 > "$OUT/gdpjump.txt" 2>&1; echo "gdpjump rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" "$SB" > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
python -X utf8 -I "$S/c127bd/metal100.py" "$INS" $SB $SE > "$OUT/metal.txt" 2>&1; echo "metal rc=$?"
python -X utf8 -I "$S/m1254/dec_after.py" "$BASE" $SB $SE > "$OUT/dec_after_base.txt" 2>&1; echo "dec_after base rc=$?"
echo READDONE
