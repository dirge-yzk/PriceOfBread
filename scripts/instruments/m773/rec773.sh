#!/usr/bin/env bash
# (773) 合流の記録の駆動（読むだけ）。腕 M・N1・N2・S11（計器つき 10 種）と c6pr の腕 B・S11 の生、基線 keep_764 の生を読む。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_m773; S=scripts/instruments; W=D:/PoB_work/m773
M=$W/out_mB_10/ins; N1=$W/out_n1_10/ins; N2=$W/out_n2_10/ins; S11=$W/out_mS_10_s11/ins
PB=D:/PoB_work/c6pr/out_c6prB_10/ins; PS=D:/PoB_work/c6pr/out_c6prS_10_s11/ins
B=D:/PoB_work/keep_764/base_ins; BN1=D:/PoB_work/keep_764/n1; BN2=D:/PoB_work/keep_764/n2; A11=D:/PoB_work/keep_764/a11
OUT=$W/rec_raw.txt; : > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec gate770_M; python -I $S/c6pr/gate770.py $M >> $OUT 2>&1
sec gate770_S11; python -I $S/c6pr/gate770.py $S11 >> $OUT 2>&1
sec rawcmp_M_vs_c6prB; python -I $S/m764/rawcmp.py $M $PB landfix_,habitcap_s >> $OUT 2>&1
sec rawcmp_S11_vs_c6prS; python -I $S/m764/rawcmp.py $S11 $PS landfix_,habitcap_s >> $OUT 2>&1
sec habitcap_used_eq_old; python -I $S/m773/habitcap_check.py $M $S11 >> $OUT 2>&1
for t in "M:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; sec "plvl_$n"; python -X utf8 -I $S/brk693/plvl_seeds.py $d >> $OUT 2>&1; done
for t in "M:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; sec "hol_$n"; python $S/hol/an_h2.py $d -v4 >> $OUT 2>&1; done
sec nullgate_M_vs_764; python $S/tools/nullgate.py $W/b10_mB_10.txt measurements/b10_20261102_m764_ins measurements/b10_20261102_m764_n1_ins measurements/b10_20261102_m764_n2_ins >> $OUT 2>&1
sec nullgate_S11_vs_764; python $S/tools/nullgate.py $W/b10_mS_10_s11.txt measurements/b10_20261102_m764_ins measurements/b10_20261102_m764_n1_ins measurements/b10_20261102_m764_n2_ins >> $OUT 2>&1
sec navf_top; python -X utf8 -I $S/navf688/top_share.py m773=$M,1 n1=$N1,1 n2=$N2,1 m764=$B,1 n1_764=$BN1,1 n2_764=$BN2,1 s11=$S11,11 a11_764=$A11,11 >> $OUT 2>&1
sec home_break_vs764null; python -X utf8 -I $S/brk693/home_break.py m773=$M n1_764=$BN1 n2_764=$BN2 m764=$B --opening-only >> $OUT 2>&1
sec home_break_new; python -X utf8 -I $S/brk693/home_break.py m773=$M n1=$N1 n2=$N2 --opening-only >> $OUT 2>&1
echo RECDONE >> $OUT
