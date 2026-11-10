#!/usr/bin/env bash
# (1022) 合流の読み（m1017/rdall1017.sh の型）：c95ub の読み手 read1020.sh（門 gates1020・止める線 stoplines1002・an_h2 -v4・gdp_zero10・popjump・plvl・ukdef・nullgate・home_break・ng）を
#   M・S11・N1・N2 に回し、生の表ごとの sha1 を線の生と突き合わせる（M↔ub の腕 B・S11↔ub の S11・Z↔pc の ident）。
#   bash rdall1022.sh [腕のタグ:種の起点 ...]（既定＝mM_10:1 mS_10_s11:11 n1_10:1 n2_10:1）
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
T=D:/PoB_work/wt_m1022; S=$T/scripts/instruments; W=D:/PoB_work/m1022_work; R=$W/read; mkdir -p $R $W/hash
L=${*:-mM_10:1 mS_10_s11:11 n1_10:1 n2_10:1}
for a in $L; do tag=${a%%:*}; sb=${a##*:}; echo "== $tag"; bash $S/c95ub/read1020.sh $W $tag $sb $R/$tag; done
H="python -X utf8 -I $S/m1017/hash1017.py"
[ -d $W/out_mM_10/ins ] && { $H $W/out_mM_10/ins $W/hash/mM.tsv; $H --cmp $W/hash/ub_armB.tsv $W/hash/mM.tsv burscan_s,clock_s > $R/rawcmp_M_vs_c95ubB.txt; cat $R/rawcmp_M_vs_c95ubB.txt; }
[ -d $W/out_mS_10_s11/ins ] && { $H $W/out_mS_10_s11/ins $W/hash/mS.tsv; $H --cmp $W/hash/ub_armS11.tsv $W/hash/mS.tsv burscan_s,clock_s > $R/rawcmp_S11_vs_c95ubS.txt; cat $R/rawcmp_S11_vs_c95ubS.txt; }
Z=D:/PoB_work/ident_m1022z/out_ib_10/ins
[ -d $Z ] && { $H $Z $W/hash/z.tsv; $H --cmp $W/hash/pc_ib.tsv $W/hash/z.tsv burscan_s,clock_s > $R/rawcmp_Z_vs_c95pc.txt; cat $R/rawcmp_Z_vs_c95pc.txt;
  python -X utf8 -I $S/c95pc/record.py $Z $T/measurements/m1015_20261110/hole.tsv $R/record_Z.txt > $R/record_Z.log 2>&1; echo "record rc=$?"; cmp $R/record_Z.txt $T/measurements/m1019_20261110_record.txt && echo "record バイト一致"; }
echo RDALLDONE
