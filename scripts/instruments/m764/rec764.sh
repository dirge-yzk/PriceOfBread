#!/usr/bin/env bash
# (764) 合流の記録の駆動（読むだけ）。⚠ (773)：中で呼ぶ c5lg/gate762.py と計器 landfix_g・landfix_s は (769) c6bk（2a726618）で削除＝回し直すなら 2d341586 以前の木で回す（過去の記録）
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_m764; S=scripts/instruments; W=D:/PoB_work/m764
M=$W/out_mB_10/ins; N1=$W/out_n1_10/ins; N2=$W/out_n2_10/ins; S11=$W/out_mS_10_s11/ins
B=D:/PoB_work/keep_757/base_ins; BN1=D:/PoB_work/keep_757/n1; BN2=D:/PoB_work/keep_757/n2; A11=D:/PoB_work/keep_757/a11
OUT=$W/rec_raw.txt; : > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec gate762; python -I $S/c5lg/gate762.py --A $W/staticA --B $M --S11 $S11 >> $OUT 2>&1
sec rawcmp_M_vs_c5lgB; python -I $S/m764/rawcmp.py $M D:/PoB_work/c5lg_B/out_c5lgB_10/ins pgrip_s >> $OUT 2>&1
sec rawcmp_S11_vs_c5lgS; python -I $S/m764/rawcmp.py $S11 D:/PoB_work/c5lg_S11/out_c5lgS_10_s11/ins pgrip_s >> $OUT 2>&1
sec landfix_g_vs_static; for f in $M/landfix_g_s*.tsv; do cmp -s $f D:/PoB_work/c5lg_work/static/landfix_g_B_s1.tsv && echo "same $(basename $f)" || echo "DIFF $(basename $f)"; done >> $OUT 2>&1
for t in "M:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; sec "plvl_$n"; python -X utf8 -I $S/brk693/plvl_seeds.py $d >> $OUT 2>&1; done
for t in "M:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; sec "hol_$n"; python $S/hol/an_h2.py $d -v4 >> $OUT 2>&1; done
sec nullgate_M_vs_757; python $S/tools/nullgate.py $W/b10_mB_10.txt measurements/b10_20261102_m757_ins measurements/b10_20261102_m757_n1_ins measurements/b10_20261102_m757_n2_ins >> $OUT 2>&1
sec nullgate_S11_vs_757; python $S/tools/nullgate.py $W/b10_mS_10_s11.txt measurements/b10_20261102_m757_ins measurements/b10_20261102_m757_n1_ins measurements/b10_20261102_m757_n2_ins >> $OUT 2>&1
sec navf_top; python -X utf8 -I $S/navf688/top_share.py m764=$M,1 n1=$N1,1 n2=$N2,1 m757=$B,1 n1_757=$BN1,1 n2_757=$BN2,1 s11=$S11,11 a11_757=$A11,11 >> $OUT 2>&1
sec home_break_vs757null; python -X utf8 -I $S/brk693/home_break.py m764=$M n1_757=$BN1 n2_757=$BN2 m757=$B --opening-only >> $OUT 2>&1
sec home_break_new; python -X utf8 -I $S/brk693/home_break.py m764=$M n1=$N1 n2=$N2 --opening-only >> $OUT 2>&1
echo RECDONE >> $OUT
