#!/usr/bin/env bash
# (802) 合流の記録の駆動（読むだけ）。腕 M・S11・N1・N2（計器つき 10 種）と c11fl の腕 B の生・基線 m797 の生（keep_797）を読む。⚠走っている間にこの台本を書き換えない（README ★(778)）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_m802; S=scripts/instruments; W=D:/PoB_work/m802
M=$W/out_mB_10/ins; N1=$W/out_n1_10/ins; N2=$W/out_n2_10/ins; S11=$W/out_mS_10_s11/ins
PB=D:/PoB_work/c11fl/out_c11flB_10/ins
B=D:/PoB_work/keep_797/base_ins; BN1=D:/PoB_work/keep_797/n1; BN2=D:/PoB_work/keep_797/n2; A11=D:/PoB_work/keep_797/a11
OUT=$W/rec_raw.txt; : > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec rawcmp_M_vs_c11flB; python -X utf8 -I $S/m764/rawcmp.py $M $PB score_s,cpeace_s,flow_s >> $OUT 2>&1
python -X utf8 -I $S/m802/colcmp.py $M $PB score_s - scoreOld,rankOld,isGreatOld >> $OUT 2>&1
python -X utf8 -I $S/m802/colcmp.py $M $PB cpeace_s repMovedIndex - >> $OUT 2>&1
sec flowcmp_M_vs_c11flB; python -X utf8 $S/m778/flowcmp.py $M $PB >> $OUT 2>&1
sec "divhost_M（門 U・L・形の一致・同じ盤面の旧新）"; SEED0=1 python -X utf8 -I $S/m802/divhost_read_s.py $M 10 >> $OUT 2>&1
sec "divhost_S11"; SEED0=11 python -X utf8 -I $S/m802/divhost_read_s.py $S11 10 >> $OUT 2>&1
for t in "M:$M:1" "S11:$S11:11" "N1:$N1:1" "N2:$N2:1" "a11_797:$A11:11"; do IFS=: read n d0 d1 s <<< "$t"; SEED0=$s python -X utf8 -I $S/m802/inv_income_s.py "$d0:$d1" 10 > $W/inv_income_$n.tsv 2>&1; done
sec "門 O"; python -X utf8 -I $S/m802/o_gate.py M=$W/inv_income_M.tsv S11=$W/inv_income_S11.tsv N1new=$W/inv_income_N1.tsv N2new=$W/inv_income_N2.tsv a11_797=$W/inv_income_a11_797.tsv base797=measurements/m801_20261103/inv_income_base.tsv n1_797=measurements/m801_20261103/inv_income_n1.tsv n2_797=measurements/m801_20261103/inv_income_n2.tsv B=measurements/m801_20261103/inv_income_B.tsv >> $OUT 2>&1
for t in "M:$M:1" "S11:$S11:11" "N1:$N1:1" "N2:$N2:1" "base797:$B:1" "n1_797:$BN1:1" "n2_797:$BN2:1" "a11_797:$A11:11"; do IFS=: read n d0 d1 s <<< "$t"; sec "ukdefault_$n"; python -X utf8 -I $S/c10cp/ukdefault.py "$d0:$d1" $s 10 >> $OUT 2>&1; done
for t in "M:$M" "N1:$N1" "N2:$N2" "S11:$S11"; do n=${t%%:*}; d=${t#*:}; sec "plvl_$n"; python -X utf8 -I $S/brk693/plvl_seeds.py $d >> $OUT 2>&1; done
sec pxjump; python -X utf8 -I $S/c11fl/pxjump.py M=$M S11=$S11 N1=$N1 N2=$N2 base797=$B n1_797=$BN1 n2_797=$BN2 a11_797=$A11 >> $OUT 2>&1
sec gdpjump; python -X utf8 -I $S/m797/gdp_jump797.py M=$M S11=$S11 N1=$N1 N2=$N2 base797=$B n1_797=$BN1 n2_797=$BN2 a11_797=$A11 >> $OUT 2>&1
for t in "M:$M" "N1:$N1" "N2:$N2" "S11:$S11"; do n=${t%%:*}; d=${t#*:}; sec "hol_$n"; python -X utf8 $S/hol/an_h2.py $d -v4 >> $OUT 2>&1; done
sec nullgate_M_vs_797; python -X utf8 $S/tools/nullgate.py $W/b10_mB_10.txt measurements/b10_20261103_m797_ins measurements/b10_20261103_m797_n1_ins measurements/b10_20261103_m797_n2_ins >> $OUT 2>&1
sec nullgate_S11_vs_797; python -X utf8 $S/tools/nullgate.py $W/b10_mS_10_s11.txt measurements/b10_20261103_m797_ins measurements/b10_20261103_m797_n1_ins measurements/b10_20261103_m797_n2_ins >> $OUT 2>&1
sec navf_top; python -X utf8 -I $S/navf688/top_share.py m802=$M,1 n1=$N1,1 n2=$N2,1 m797=$B,1 n1_797=$BN1,1 n2_797=$BN2,1 s11=$S11,11 a11_797=$A11,11 >> $OUT 2>&1
sec home_break_new; python -X utf8 -I $S/brk693/home_break.py m802=$M n1=$N1 n2=$N2 --opening-only >> $OUT 2>&1
echo RECDONE >> $OUT
