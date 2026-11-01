#!/usr/bin/env bash
# (749) 合流の記録の駆動（読むだけ）。rec702.sh の汎用の部分の写し（plvl・hol・cap・judge・nat・本国の崩れ・錠・首位の取り分）＋波53 の読み手（gate748・nearflow・flows748）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_m749; S=scripts/instruments; W=D:/PoB_work/m749
M=$W/out_mB_10/ins; N1=$W/out_n1_10/ins; N2=$W/out_n2_10/ins; S11=$W/out_mS_10_s11/ins
B=D:/PoB_work/keep_736/base_ins; BN1=D:/PoB_work/keep_736/n1; BN2=D:/PoB_work/keep_736/n2; A11=D:/PoB_work/keep_736/a11
OUT=$W/rec_raw.txt; : > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
for t in "M:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; [ -d $d ] || continue; sec "plvl_$n"; python -X utf8 -I $S/brk693/plvl_seeds.py $d >> $OUT 2>&1; done
for t in "M:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; [ -d $d ] || continue; sec "hol_$n"; python $S/hol/an_h2.py $d -v4 >> $OUT 2>&1; done
for t in "M:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; [ -d $d ] || continue; sec "gate748_$n"; python $S/c3h1/gate748.py $d >> $OUT 2>&1; sec "nearflow_$n"; python $S/m749/nearflow.py $d >> $OUT 2>&1; done
sec flows_M; python $S/m749/flows748.py $M $N1 $N2 $B >> $OUT 2>&1
sec flows_S11; SEED0=11 python $S/m749/flows748.py $S11 $A11 >> $OUT 2>&1
sec cap; python -X utf8 -I $S/sm7_620/an_cap.py $M $B $N1 $N2 >> $OUT 2>&1
sec judge; python $S/igi4_667/judge.py m702=$B m749=$M n1=$N1 n2=$N2 >> $OUT 2>&1
sec nat; python -X utf8 -I $S/m655/rec_nat655.py m749=$M n1=$N1 n2=$N2 m702=$B >> $OUT 2>&1
sec home_break_old_nulls; python -X utf8 -I $S/brk693/home_break.py m749=$M m702=$B n1=$BN1 n2=$BN2 --opening-only --test m749 >> $OUT 2>&1
sec home_break_new; python -X utf8 -I $S/brk693/home_break.py m749=$M n1=$N1 n2=$N2 --opening-only >> $OUT 2>&1
sec home_break_s11; python -X utf8 -I $S/brk693/home_break.py s11=$S11 a11_702=$A11 --opening-only --test s11 >> $OUT 2>&1
sec lock3; python -X utf8 -I $S/m702/lock3.py M=$M m702=$B N1=$BN1 N2=$BN2 S11=$S11,11 A11=$A11,11 --pairs "M:m702,N1,N2;S11:A11;m702:N1,N2;N1:m702,N2;N2:m702,N1" >> $OUT 2>&1
sec navf_top; python -X utf8 -I $S/navf688/top_share.py m749=$M,1 n1=$N1,1 n2=$N2,1 m702=$B,1 s11=$S11,11 a11_702=$A11,11 >> $OUT 2>&1
echo RECDONE >> $OUT
