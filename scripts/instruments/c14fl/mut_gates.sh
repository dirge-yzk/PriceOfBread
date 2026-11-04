#!/usr/bin/env bash
# (815) c14fl 突然変異の腕の門（読むだけ）：bash mut_gates.sh <出力> <旗 0 の ins（ident の腕 b の生）> <Mu0 Mu1 ...>
#   各腕（計器つき 2 種・種 1〜2）に gate815.py --short を当てる（G1・G2 だけ）。G5 は gdp_zero10.py。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c14fl; S=scripts/instruments
OUT=${1:?出力}; Z=${2:?旗 0 の ins}; shift 2
: > $OUT
for m in "$@"; do
  I=D:/PoB_work/c14fl_work/mut/out_c14fl${m}_2/ins
  echo "## ===== $m（$I）" >> $OUT
  python -X utf8 -I $S/c14fl/gate815.py $I $Z D:/PoB_work/keep_813/base_ins 1,2 --short >> $OUT 2>&1
  python -X utf8 -I $S/m808/gdp_zero10.py $m=$I >> $OUT 2>&1
done
echo "## ===== 旗 0（同じ読み手・種 1〜2）" >> $OUT
python -X utf8 -I $S/c14fl/gate815.py $Z $Z D:/PoB_work/keep_813/base_ins 1,2 --short >> $OUT 2>&1
echo MUTGATESDONE >> $OUT
