#!/usr/bin/env bash
# (988) 合流の読みの順（rd2981.sh の型）：ヌル腕 n1・n2 → M・S11（新しいヌル腕を NULLS に）→ c89ls の no-go（M・N1・N2）。
cd D:/PoB_work/wt_m988 || exit 9
W=D:/PoB_work/m988_work; S=scripts/instruments/m988
for a in n1 n2; do echo "== $a"; bash $S/read988.sh $a $W/out_${a}_10 $W/b10_${a}_10.txt 1; done
echo "== mM"; bash $S/read988.sh mM $W/out_mM_10 $W/b10_mM_10.txt 1 newnulls
echo "== mS"; bash $S/read988.sh mS $W/out_mS_10_s11 $W/b10_mS_10_s11.txt 11 newnulls
python -X utf8 -I scripts/instruments/c89ls/gates983.py nogo $W/out_mM_10/ins $W/out_n1_10/ins $W/out_n2_10/ins > $W/read/nogo983.txt 2>&1; echo "nogo rc=$?"
echo RDALLDONE
