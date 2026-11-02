#!/usr/bin/env bash
# (778) 合流の記録の駆動（読むだけ）。腕 Z（旗 0）・M・S11・N1・N2（計器つき 10 種）と線の生・基線 keep_773 の生を読む。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_m778; S=scripts/instruments; W=D:/PoB_work/m778
Z=$W/out_mZ_10/ins; M=$W/out_mB_10/ins; N1=$W/out_n1_10/ins; N2=$W/out_n2_10/ins; S11=$W/out_mS_10_s11/ins
HB=D:/PoB_work/c7hl_arm/out_c7hlB_10/ins
B=D:/PoB_work/keep_773/base_ins; BN1=D:/PoB_work/keep_773/n1; BN2=D:/PoB_work/keep_773/n2; A11=D:/PoB_work/keep_773/a11
OUT=$W/rec_raw.txt; : > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec rawcmp_Z_vs_c7hlB; python -X utf8 -I $S/m764/rawcmp.py $Z $HB portlm_s,inheritintent_s,silverpar_s >> $OUT 2>&1
# c7sw の門（gate774.py）：Z の値を旗 0 の基準に
sec gate774_Z; python -X utf8 -I $S/c7sw/gate774.py $Z > $W/gate774_Z.txt 2>&1; cat $W/gate774_Z.txt >> $OUT
X0=$(grep -o 'L1（結合・種の和）=[0-9]*' $W/gate774_Z.txt | grep -o '[0-9]*$'); P0=$(grep -o '平均（年末×国×種）=[0-9.]*' $W/gate774_Z.txt | grep -o '[0-9.]*$')
echo "# Z から取った x0=$X0 pend0=$P0" >> $OUT
sec gate774_M; python -X utf8 -I $S/c7sw/gate774.py $M --x0 $X0 --pend0 $P0 >> $OUT 2>&1
sec gate774_S11; python -X utf8 -I $S/c7sw/gate774.py $S11 --seeds 11-20 --x0 $X0 --pend0 $P0 >> $OUT 2>&1
# c7ag の門（gates775.py）：Z の Σ fiscalBAt3Count を L2 の基準に
sec gates775_Z; python -X utf8 -I $S/c7ag/gates775.py $Z --tag Z > $W/gates775_Z.txt 2>&1; cat $W/gates775_Z.txt >> $OUT
L2B=$(grep -o 'Σ=[0-9.]*' $W/gates775_Z.txt | head -1 | grep -o '[0-9.]*$')
echo "# Z から取った l2base=$L2B" >> $OUT
sec gates775_M; python -X utf8 -I $S/c7ag/gates775.py $M --l2base $L2B --tag M >> $OUT 2>&1
sec gates775_S11; python -X utf8 -I $S/c7ag/gates775.py $S11 --seeds 11-20 --l2base $L2B --tag S11 >> $OUT 2>&1
sec rec_lines; python -X utf8 -I $S/m778/rec_lines.py Z=$Z M=$M S11=$S11,11 N1=$N1 N2=$N2 >> $OUT 2>&1
for t in "Z:$Z" "M:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; sec "plvl_$n"; python -X utf8 -I $S/brk693/plvl_seeds.py $d >> $OUT 2>&1; done
for t in "Z:$Z" "M:$M" "n1:$N1" "n2:$N2" "s11:$S11"; do n=${t%%:*}; d=${t#*:}; sec "hol_$n"; python -X utf8 $S/hol/an_h2.py $d -v4 >> $OUT 2>&1; done
sec nullgate_M_vs_773; python -X utf8 $S/tools/nullgate.py $W/b10_mB_10.txt measurements/b10_20261102_m773_ins measurements/b10_20261102_m773_n1_ins measurements/b10_20261102_m773_n2_ins >> $OUT 2>&1
sec nullgate_S11_vs_773; python -X utf8 $S/tools/nullgate.py $W/b10_mS_10_s11.txt measurements/b10_20261102_m773_ins measurements/b10_20261102_m773_n1_ins measurements/b10_20261102_m773_n2_ins >> $OUT 2>&1
sec navf_top; python -X utf8 -I $S/navf688/top_share.py m778=$M,1 n1=$N1,1 n2=$N2,1 m773=$B,1 n1_773=$BN1,1 n2_773=$BN2,1 s11=$S11,11 a11_773=$A11,11 >> $OUT 2>&1
sec home_break_new; python -X utf8 -I $S/brk693/home_break.py m778=$M n1=$N1 n2=$N2 --opening-only >> $OUT 2>&1
sec home_break_vs773null; python -X utf8 -I $S/brk693/home_break.py m778=$M n1_773=$BN1 n2_773=$BN2 m773=$B --opening-only >> $OUT 2>&1
sec static776; python -X utf8 -I $S/m778/static776.py $W/static/sea_old.bin PriceOfBread.SimCore/landmask.bin $W/static/land_old.bin PriceOfBread.SimCore/landmask_land.bin >> $OUT 2>&1
sec paper_pg_M; python -X utf8 -I measurements/m777_20261102_paper_pg.py --landmass PriceOfBread.SimCore/landmass.json --portch $M --portch $S11:11 --portlm $M --portlm $S11 --out $W/paper_pg_M.txt >> $OUT 2>&1
echo RECDONE >> $OUT
