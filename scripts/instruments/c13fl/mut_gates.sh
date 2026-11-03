#!/usr/bin/env bash
# (810) c13fl 突然変異の腕の門（読むだけ）：bash mut_gates.sh <出力> <旗 0 の ins（ident の腕 b の生）> <Mu1 Mu2 ...>
#   各腕（計器つき 2 種・種 1〜2）に gate810.py を当てる（G6 の比べる側は旗 0 の同じ種 1〜2）。G7 は gdp_zero10.py。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c13fl; S=scripts/instruments
OUT=${1:?出力}; Z=${2:?旗 0 の ins}; shift 2
: > $OUT
for m in "$@"; do
  I=D:/PoB_work/c13fl_work/mut/out_c13fl${m}_2/ins
  echo "## ===== $m（$I）" >> $OUT
  python -X utf8 -I $S/c13fl/gate810.py $I 1,2 --base $Z 1,2 >> $OUT 2>&1
  python -X utf8 -I $S/m808/gdp_zero10.py $m=$I >> $OUT 2>&1
done
echo "## ===== 旗 0（同じ読み手・種 1〜2）" >> $OUT
python -X utf8 -I $S/c13fl/gate810.py $Z 1,2 --base $Z 1,2 >> $OUT 2>&1
echo MUTGATESDONE >> $OUT
