#!/usr/bin/env bash
# (1001) 合流の読みの順（m988/rdall988.sh の写し）：ヌル腕 n1・n2 → M・S11（新しいヌル腕を NULLS に）（生の比べ M↔c91sl 腕 B・S11↔c91sl 腕 S11 は別に bg.sh で先に回した）。
cd D:/PoB_work/wt_m1001 || exit 9
W=D:/PoB_work/m1001_work; S=scripts/instruments/m1001; C=D:/PoB_work/c91sl_work
for a in n1 n2; do echo "== $a"; bash $S/read1001.sh $a $W/out_${a}_10 $W/b10_${a}_10.txt 1 "" nogate; done
echo "== mM"; bash $S/read1001.sh mM $W/out_mM_10 $W/b10_mM_10.txt 1 newnulls
echo "== mS"; bash $S/read1001.sh mS $W/out_mS_10_s11 $W/b10_mS_10_s11.txt 11 newnulls
echo RDALLDONE
