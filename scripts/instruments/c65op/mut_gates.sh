#!/usr/bin/env bash
# (820) c65op 突然変異の腕の門（読むだけ）：bash mut_gates.sh <出力> <旗 0 の ins（ident の腕 b の生）> <Mu1 Mu2 ...>
#   各腕（計器つき 2 種・種 1〜2）に gate_op.py を当てる。旗 0（Mu0＝旗を読まない経路）は ident の腕 b の生（10 種）と、その種 1〜2 だけの写しで読む。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c65op; S=scripts/instruments
OUT=${1:?出力}; Z=${2:?旗 0 の ins}; shift 2
: > $OUT
for m in "$@"; do
  I=D:/PoB_work/c65op_work/mut/out_c65op${m}_2/ins
  echo "## ===== $m（$I）" >> $OUT
  python -X utf8 -I $S/c65op/gate_op.py $I $m >> $OUT 2>&1
done
echo "## ===== Mu0＝旗 0（ident の腕 b・10 種）" >> $OUT
python -X utf8 -I $S/c65op/gate_op.py $Z flag0 >> $OUT 2>&1
Z2=D:/PoB_work/c65op_work/flag0_s12; rm -rf $Z2; mkdir -p $Z2; cp $Z/habitlg_s1.tsv $Z/habitlg_s2.tsv $Z2/
echo "## ===== Mu0＝旗 0（同じ生の種 1〜2 だけ）" >> $OUT
python -X utf8 -I $S/c65op/gate_op.py $Z2 flag0_s12 >> $OUT 2>&1
echo MUTGATESDONE >> $OUT
