#!/usr/bin/env bash
# (694) 合流の記録の駆動（読むだけ）
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_m694; S=scripts/instruments
M=D:/PoB_work/m694/out_m694i_10/ins; N1=D:/PoB_work/m694/out_n1_10/ins; N2=D:/PoB_work/m694/out_n2_10/ins; B=D:/PoB_work/keep_685/m685_ins
U0=D:/PoB_work/m694/out_ab_u0_10/ins; A0=D:/PoB_work/m694/out_ab_n0_10/ins; S11=D:/PoB_work/m694/out_s11_10_s11/ins; A11=D:/PoB_work/keep_685/a11
OUT=D:/PoB_work/m694/rec_raw.txt; : > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
for t in "ins:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; sec "plvl_$n"; if [ $n = s11 ]; then python -X utf8 -I $S/brk693/plvl_seeds.py $d >> $OUT 2>&1; else python -X utf8 -I $S/m609/plvl_all.py $d 10 $n >> $OUT 2>&1; python -X utf8 -I $S/brk693/plvl_seeds.py $d >> $OUT 2>&1; fi; done
for t in "ins:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; sec "hol_$n"; python $S/hol/an_h2.py $d -v4 >> $OUT 2>&1; done
sec cap; python -X utf8 -I $S/sm7_620/an_cap.py $M $B $N1 $N2 >> $OUT 2>&1
sec cap_n1; python -X utf8 -I $S/sm7_620/an_cap.py $N1 $M >> $OUT 2>&1
sec cap_n2; python -X utf8 -I $S/sm7_620/an_cap.py $N2 $M >> $OUT 2>&1
sec cap_urb2_vs_u0; python -X utf8 -I $S/sm7_620/an_cap.py $M $U0 >> $OUT 2>&1
sec judge; python $S/igi4_667/judge.py m685=$B m694=$M n1=$N1 n2=$N2 >> $OUT 2>&1
sec urb; python -X utf8 -I $S/m675/rec_urb675.py m694=$M n1=$N1 n2=$N2 m685=$B u0=$U0 >> $OUT 2>&1
sec nat; python -X utf8 -I $S/m655/rec_nat655.py m694=$M n1=$N1 n2=$N2 m685=$B >> $OUT 2>&1
sec gold; python $S/m560/gold.py $M $N1 $N2 $B >> $OUT 2>&1
sec edu_ins; python -I $S/edu/an_edu.py $M >> $OUT 2>&1
sec prdf; python -I $S/prdf/an_prdf.py $M >> $OUT 2>&1
sec break; python $S/m675/break675.py m694=$M N1=$N1 N2=$N2 m685=$B >> $OUT 2>&1
sec home_break; python -X utf8 -I $S/brk693/home_break.py m694=$M n1=$N1 n2=$N2 m685=$B --opening-only >> $OUT 2>&1
sec home_break_s11; python -X utf8 -I $S/brk693/home_break.py m694=$M n1=$N1 n2=$N2 s11=$S11 a11_685=$A11 --opening-only --test s11 >> $OUT 2>&1
for r in m617/rec617 m627/rec627 m609/rec609 m645/rec645 m592/rec592; do sec $(basename $r); python -X utf8 -I $S/$r.py m694=$M n1=$N1 n2=$N2 m685=$B >> $OUT 2>&1; done
sec urb2_rec3; python -X utf8 -I $S/urb2_687/rec3.py $M >> $OUT 2>&1; python -X utf8 -I $S/urb2_687/rec3.py $U0 >> $OUT 2>&1
sec urb2_gates_s11; sed "s#D:/PoB_work/urb2_687/out_B_10_s11/ins#$S11#" $S/urb2_687/gates3_s11.py > $S/urb2_687/_g11_694.py; python -X utf8 -I $S/urb2_687/_g11_694.py >> $OUT 2>&1; rm -f $S/urb2_687/_g11_694.py
sec navf_fleet; python -X utf8 -I $S/navf688/fleet_rec.py m694=$M,1 n1=$N1,1 n2=$N2,1 m685=$B,1 s11=$S11,11 a11_685=$A11,11 >> $OUT 2>&1
sec navf_top; python -X utf8 -I $S/navf688/top_share.py m694=$M,1 n1=$N1,1 n2=$N2,1 m685=$B,1 s11=$S11,11 a11_685=$A11,11 >> $OUT 2>&1
echo RECDONE >> $OUT
