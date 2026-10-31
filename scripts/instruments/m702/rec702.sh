#!/usr/bin/env bash
# (702) 合流の記録の駆動（読むだけ）。rec694.sh の写し＋この波の線の読み手（hcd・ins7・prd1・rd47・③の錠）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_m702; S=scripts/instruments; W=D:/PoB_work/m702
M=$W/out_M_10/ins; A0=$W/out_A0_10/ins; N1=$W/out_N1_10/ins; N2=$W/out_N2_10/ins; S11=$W/out_S11_10_s11/ins
B=D:/PoB_work/keep_694/m694_ins; BN1=D:/PoB_work/keep_694/n1; BN2=D:/PoB_work/keep_694/n2; A11=D:/PoB_work/keep_694/a11
OUT=$W/rec_raw.txt; : > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
for t in "M:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; [ -d $d ] || continue; sec "plvl_$n"; python -X utf8 -I $S/brk693/plvl_seeds.py $d >> $OUT 2>&1; done
for t in "M:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; [ -d $d ] || continue; sec "hol_$n"; python $S/hol/an_h2.py $d -v4 -ex old >> $OUT 2>&1; done
sec cap; python -X utf8 -I $S/sm7_620/an_cap.py $M $B $N1 $N2 >> $OUT 2>&1
sec cap_vs_A0; python -X utf8 -I $S/sm7_620/an_cap.py $M $A0 >> $OUT 2>&1
sec judge; python $S/igi4_667/judge.py m694=$B m702=$M n1=$N1 n2=$N2 >> $OUT 2>&1
sec urb; python -X utf8 -I $S/m675/rec_urb675.py m702=$M n1=$N1 n2=$N2 m694=$B >> $OUT 2>&1
sec nat; python -X utf8 -I $S/m655/rec_nat655.py m702=$M n1=$N1 n2=$N2 m694=$B >> $OUT 2>&1
sec gold; python $S/m560/gold.py $M $N1 $N2 $B >> $OUT 2>&1
sec break; python $S/m675/break675.py m702=$M N1=$N1 N2=$N2 m694=$B >> $OUT 2>&1
sec home_break_old_nulls; python -X utf8 -I $S/brk693/home_break.py m702=$M m694=$B n1=$BN1 n2=$BN2 --opening-only --test m702 >> $OUT 2>&1
sec home_break_new; python -X utf8 -I $S/brk693/home_break.py m702=$M n1=$N1 n2=$N2 --opening-only >> $OUT 2>&1
sec home_break_s11; python -X utf8 -I $S/brk693/home_break.py s11=$S11 a11_694=$A11 --opening-only --test s11 >> $OUT 2>&1
sec lock3; python -X utf8 -I $S/m702/lock3.py M=$M m694=$B N1=$BN1 N2=$BN2 S11=$S11,11 A11=$A11,11 --pairs "M:m694,N1,N2;S11:A11;m694:N1,N2;N1:m694,N2;N2:m694,N1" >> $OUT 2>&1
sec an_brk; python -X utf8 -I $S/brk/an_brk.py M=$M m694=$B N1=$BN1 N2=$BN2 >> $OUT 2>&1
for r in m617/rec617 m627/rec627 m609/rec609 m645/rec645 m592/rec592; do sec $(basename $r); python -X utf8 -I $S/$r.py m702=$M n1=$N1 n2=$N2 m694=$B >> $OUT 2>&1; done
sec colxfer_M; python $S/hcd/colxfer.py PriceOfBread.SimCore/places.json PriceOfBread.SimCore/world.json $W/colx_M.tsv $M >> $OUT 2>&1
for t in "N1:$N1" "N2:$N2" "S11:$S11" "A11:$A11"; do n=${t%%:*}; d=${t#*:}; [ -d $d ] || continue; sec "colxfer_$n"; python $S/hcd/colxfer.py PriceOfBread.SimCore/places.json PriceOfBread.SimCore/world.json $W/colx_$n.tsv $d >> $OUT 2>&1; done
sec roundtrip; python -X utf8 -I $S/ins7/roundtrip.py $M >> $OUT 2>&1
sec shadow; python -X utf8 -I $S/prd1/shadow.py $M >> $OUT 2>&1
sec first_idea; python -X utf8 -I $S/rd47/first_idea.py m702=$M n1=$N1 n2=$N2 m694=$B 2>&1 | grep -v "^D" | head -400 >> $OUT
sec navf_top; python -X utf8 -I $S/navf688/top_share.py m702=$M,1 n1=$N1,1 n2=$N2,1 m694=$B,1 s11=$S11,11 a11_694=$A11,11 >> $OUT 2>&1
echo RECDONE >> $OUT
