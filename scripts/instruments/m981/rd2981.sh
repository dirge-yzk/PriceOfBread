#!/usr/bin/env bash
# (981) 合流（m975 の rd2.sh の写し）：S11 の読み（新しい N1・N2 も NULLS に）と M の stop949 を新しいヌル腕で数え直す。
cd D:/PoB_work/wt_m981 || exit 9
W=D:/PoB_work/m981_work
bash scripts/instruments/m981/read981.sh mS $W/out_mS_10_s11 $W/b10_mS_10_s11.txt 11 newnulls
R=$W/read/mM; RULE=pair NULLS="$W/read/n1/stop906.txt $W/read/n2/stop906.txt" bash scripts/instruments/c84gz/stoplines949.sh $W/out_mM_10/ins $R/stop949_newnulls.txt 1 > $R/stop949_newnulls.log 2>&1; echo "mM stop949new rc=$?"
echo RD2DONE
