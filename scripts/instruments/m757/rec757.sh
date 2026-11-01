#!/usr/bin/env bash
# (757) 合流の記録の駆動（読むだけ）
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_m757; S=scripts/instruments; W=D:/PoB_work/m757
M=$W/out_mB_10/ins; N1=$W/out_n1_10/ins; N2=$W/out_n2_10/ins; S11=$W/out_mS_10_s11/ins
B=D:/PoB_work/keep_749/base_ins; BN1=D:/PoB_work/keep_749/n1; BN2=D:/PoB_work/keep_749/n2; A11=D:/PoB_work/keep_749/a11
OUT=$W/rec_raw.txt; : > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec gate756_M; python $S/c4dg/gate756.py $M >> $OUT 2>&1
sec gate756_S11; python $S/c4dg/gate756.py $S11 >> $OUT 2>&1
sec gate756_N1; python $S/c4dg/gate756.py $N1 >> $OUT 2>&1
sec gate756_N2; python $S/c4dg/gate756.py $N2 >> $OUT 2>&1
for t in "M:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; sec "plvl_$n"; python -X utf8 -I $S/brk693/plvl_seeds.py $d >> $OUT 2>&1; done
for t in "M:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; sec "hol_$n"; python $S/hol/an_h2.py $d -v4 >> $OUT 2>&1; done
sec nullgate_M_vs_749; python $S/tools/nullgate.py $W/b10_mB_10.txt measurements/b10_20261102_m749_ins measurements/b10_20261102_m749_n1_ins measurements/b10_20261102_m749_n2_ins >> $OUT 2>&1
sec flows_M; python $S/m749/flows748.py $M $N1 $N2 $B >> $OUT 2>&1
sec navf_top; python -X utf8 -I $S/navf688/top_share.py m757=$M,1 n1=$N1,1 n2=$N2,1 m749=$B,1 s11=$S11,11 a11_749=$A11,11 >> $OUT 2>&1
sec home_break_new; python -X utf8 -I $S/brk693/home_break.py m757=$M n1=$N1 n2=$N2 --opening-only >> $OUT 2>&1
echo RECDONE >> $OUT
