#!/usr/bin/env bash
# (784) 合流の記録の駆動（読むだけ）。腕 M・S11・N1・N2（計器つき 10 種）と c8pd の腕 B の生・基線 keep_778 の生を読む。
#   ⚠走っている間にこの台本を書き換えない（README ★(778)）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_m784; S=scripts/instruments; W=D:/PoB_work/m784
M=$W/out_mB_10/ins; N1=$W/out_n1_10/ins; N2=$W/out_n2_10/ins; S11=$W/out_mS_10_s11/ins
PB=D:/PoB_work/c8pd/B/out_c8pdB_10/ins
B=D:/PoB_work/keep_778/base_ins; BN1=D:/PoB_work/keep_778/n1; BN2=D:/PoB_work/keep_778/n2; A11=D:/PoB_work/keep_778/a11
OUT=$W/rec_raw.txt; : > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec rawcmp_M_vs_c8pdB; python -X utf8 -I $S/m764/rawcmp.py $M $PB inheritintent_s,inheritpar_s >> $OUT 2>&1
sec flowcmp_M_vs_c8pdB; python -X utf8 $S/m778/flowcmp.py $M $PB >> $OUT 2>&1
sec gate779_M; python -X utf8 -I $S/c8pd/gate.py $M --detail --out $W/gate779_M.txt >> $OUT 2>&1; echo "rc=$?" >> $OUT
sec gate779_S11; python -X utf8 -I $S/c8pd/gate.py $S11 --detail --out $W/gate779_S11.txt >> $OUT 2>&1; echo "rc=$?" >> $OUT
sec count_ii_M; python -X utf8 -I measurements/m781_20261102_count_ii.py $M M >> $OUT 2>&1
sec count_ii_S11; python -X utf8 -I measurements/m781_20261102_count_ii.py $S11 S11 >> $OUT 2>&1
sec rec_lines; python -X utf8 -I $S/m778/rec_lines.py M=$M S11=$S11,11 N1=$N1 N2=$N2 >> $OUT 2>&1
for t in "M:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; sec "plvl_$n"; python -X utf8 -I $S/brk693/plvl_seeds.py $d >> $OUT 2>&1; done
for t in "M:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; sec "hol_$n"; python -X utf8 $S/hol/an_h2.py $d -v4 >> $OUT 2>&1; done
sec nullgate_M_vs_778; python -X utf8 $S/tools/nullgate.py $W/b10_mB_10.txt measurements/b10_20261102_m778_ins measurements/b10_20261102_m778_n1_ins measurements/b10_20261102_m778_n2_ins >> $OUT 2>&1
sec nullgate_S11_vs_778; python -X utf8 $S/tools/nullgate.py $W/b10_mS_10_s11.txt measurements/b10_20261102_m778_ins measurements/b10_20261102_m778_n1_ins measurements/b10_20261102_m778_n2_ins >> $OUT 2>&1
sec navf_top; python -X utf8 -I $S/navf688/top_share.py m784=$M,1 n1=$N1,1 n2=$N2,1 m778=$B,1 n1_778=$BN1,1 n2_778=$BN2,1 s11=$S11,11 a11_778=$A11,11 >> $OUT 2>&1
sec home_break_new; python -X utf8 -I $S/brk693/home_break.py m784=$M n1=$N1 n2=$N2 --opening-only >> $OUT 2>&1
echo RECDONE >> $OUT
