#!/usr/bin/env bash
# (797) 合流の記録の駆動（読むだけ）。腕 M・S11・N1・N2（計器つき 10 種）と c10cp の腕 B2 の生・基線の生を読む。⚠走っている間にこの台本を書き換えない（README ★(778)）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_m797; S=scripts/instruments; W=D:/PoB_work/m797
M=$W/out_mB_10/ins; N1=$W/out_n1_10/ins; N2=$W/out_n2_10/ins; S11=$W/out_mS_10_s11/ins
PB=D:/PoB_work/c10cp/out_c10cpB2_10/ins
B=D:/PoB_work/keep_790/base_ins; BN1=D:/PoB_work/keep_784/n1; BN2=D:/PoB_work/keep_784/n2; A11=D:/PoB_work/keep_784/a11
OUT=$W/rec_raw.txt; : > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec rawcmp_M_vs_c10cpB2; python -X utf8 -I $S/m764/rawcmp.py $M $PB portch_s,flow_s >> $OUT 2>&1; python -X utf8 -I $S/m797/portchcmp.py $M $PB >> $OUT 2>&1
sec flowcmp_M_vs_c10cpB2; python -X utf8 $S/m778/flowcmp.py $M $PB >> $OUT 2>&1
sec gates_M; python -X utf8 -I $S/c10cp/gates.py $M --p0 0.782 --l1 0.60 >> $OUT 2>&1
sec newold; python -X utf8 -I $S/m797/newold797.py M=$M S11=$S11 >> $OUT 2>&1
sec gates_S11; python -X utf8 -I $S/c10cp/gates.py $S11 --p0 0.782 --l1 0.60 >> $OUT 2>&1
for t in "M:$M:1" "s11:$S11:11" "n1:$N1:1" "n2:$N2:1" "base784:$B:1" "n1_784:$BN1:1" "n2_784:$BN2:1" "a11_784:$A11:11"; do IFS=: read n d0 d1 s <<< "$t"; d="$d0:$d1"; sec "ukdefault_$n"; python -X utf8 -I $S/c10cp/ukdefault.py $d $s 10 >> $OUT 2>&1; done
for t in "M:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; sec "plvl_$n"; python -X utf8 -I $S/brk693/plvl_seeds.py $d >> $OUT 2>&1; done
sec gdpjump; python -X utf8 -I $S/m797/gdp_jump797.py M=$M S11=$S11 N1=$N1 N2=$N2 base784=$B n1_784=$BN1 n2_784=$BN2 a11_784=$A11 >> $OUT 2>&1
for t in "M:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; sec "hol_$n"; python -X utf8 $S/hol/an_h2.py $d -v4 >> $OUT 2>&1; done
sec nullgate_M_vs_784; python -X utf8 $S/tools/nullgate.py $W/b10_mB_10.txt measurements/b10_20261102_m784_ins measurements/b10_20261102_m784_n1_ins measurements/b10_20261102_m784_n2_ins >> $OUT 2>&1
sec nullgate_S11_vs_784; python -X utf8 $S/tools/nullgate.py $W/b10_mS_10_s11.txt measurements/b10_20261102_m784_ins measurements/b10_20261102_m784_n1_ins measurements/b10_20261102_m784_n2_ins >> $OUT 2>&1
sec navf_top; python -X utf8 -I $S/navf688/top_share.py m797=$M,1 n1=$N1,1 n2=$N2,1 m784=$B,1 n1_784=$BN1,1 n2_784=$BN2,1 s11=$S11,11 a11_784=$A11,11 >> $OUT 2>&1
sec home_break_new; python -X utf8 -I $S/brk693/home_break.py m797=$M n1=$N1 n2=$N2 --opening-only >> $OUT 2>&1
echo RECDONE >> $OUT
