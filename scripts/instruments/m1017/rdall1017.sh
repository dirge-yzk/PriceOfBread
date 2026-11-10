#!/usr/bin/env bash
# (1017) 合流の読み（読むだけ）：腕 M・S11・N1・N2 を c94pe の読み手 read1014.sh（門 gates1014・stoplines1002・an_h2 -v4・gdp_zero10・plvl・ukdef・nullgate・home_break・ng・.cost）で読み、
#   人口の比の行（popjump1017・記録）・pay_s の枝（paydiff1017・記録）・生の表ごとの sha1 を c94pe の腕の生と突き合わせる（hash1017 --cmp）。
#   bash rdall1017.sh [腕のタグ...]（既定＝mM_10:1 mS_10_s11:11 n1_10:1 n2_10:1）
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
T=D:/PoB_work/wt_m1017; S=$T/scripts/instruments; W=D:/PoB_work/m1017_work; R=$W/read; mkdir -p $R
L=${*:-mM_10:1 mS_10_s11:11 n1_10:1 n2_10:1}
for a in $L; do tag=${a%%:*}; sb=${a##*:}; echo "== $tag"; bash $S/c94pe/read1014.sh $W $tag $sb $R/$tag; done
H="python -X utf8 -I $S/m1017/hash1017.py"
[ -d $W/out_mM_10/ins ] && { $H $W/out_mM_10/ins $W/hash/mM.tsv; $H --cmp $W/hash/pe_armB.tsv $W/hash/mM.tsv burscan_s,clock_s > $R/rawcmp_M_vs_c94peB.txt; cat $R/rawcmp_M_vs_c94peB.txt; }
[ -d $W/out_mS_10_s11/ins ] && { $H $W/out_mS_10_s11/ins $W/hash/mS.tsv; $H --cmp $W/hash/pe_armS11.tsv $W/hash/mS.tsv burscan_s,clock_s > $R/rawcmp_S11_vs_c94peS.txt; cat $R/rawcmp_S11_vs_c94peS.txt; }
python -X utf8 -I $S/m1017/popjump1017.py M=$W/out_mM_10/ins N1=$W/out_n1_10/ins N2=$W/out_n2_10/ins base1011=D:/PoB_work/keep_1011/base_ins n1_1001=D:/PoB_work/keep_1001/n1 n2_1001=D:/PoB_work/keep_1001/n2 > $R/popjump_s1.txt
python -X utf8 -I $S/m1017/popjump1017.py S11=$W/out_mS_10_s11/ins a11_1001=D:/PoB_work/keep_1001/a11 > $R/popjump_s11.txt
grep -v "^   行" $R/popjump_s1.txt $R/popjump_s11.txt
python -X utf8 -I $S/m1017/paydiff1017.py base1011=D:/PoB_work/keep_1011/base_ins M=$W/out_mM_10/ins N1=$W/out_n1_10/ins N2=$W/out_n2_10/ins > $R/paydiff.txt; cut -c1-300 $R/paydiff.txt
echo RDALLDONE
