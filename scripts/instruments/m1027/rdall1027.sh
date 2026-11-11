#!/usr/bin/env bash
# (1027) 合流の読み（m1022/rdall1022.sh の型）：c96rd の読み手 read1025.sh（門 gates1025・止める線 stoplines1002・an_h2 -v4・gdp_zero10・popjump・plvl・ukdef・nullgate・home_break・ng）を
#   M・S11・N1・N2 に回し、生の表ごとの sha1 を線の生と突き合わせる（M↔rd の腕 B・S11↔rd の S11・Z↔wn と kf の ident）。
#   bash rdall1027.sh [腕のタグ:種の起点 ...]（既定＝mM_10:1 mS_10_s11:11 n1_10:1 n2_10:1）
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
T=D:/PoB_work/wt_m1027; S=$T/scripts/instruments; W=D:/PoB_work/m1027_work; R=$W/read; mkdir -p $R $W/hash
L=${*:-mM_10:1 mS_10_s11:11 n1_10:1 n2_10:1}
for a in $L; do tag=${a%%:*}; sb=${a##*:}; echo "== $tag"; bash $S/c96rd/read1025.sh $W $tag $sb $R/$tag; done
H="python -X utf8 -I $S/m1017/hash1017.py"
for a in $L; do tag=${a%%:*}
  case $tag in
    mM_10) [ -d $W/out_mM_10/ins ] && { $H $W/out_mM_10/ins $W/hash/mM.tsv; $H --cmp $W/hash/rd_armB.tsv $W/hash/mM.tsv burscan_s,clock_s > $R/rawcmp_M_vs_c96rdB.txt; cat $R/rawcmp_M_vs_c96rdB.txt; } ;;
    mS_10_s11) [ -d $W/out_mS_10_s11/ins ] && { $H $W/out_mS_10_s11/ins $W/hash/mS.tsv; $H --cmp $W/hash/rd_armS11.tsv $W/hash/mS.tsv burscan_s,clock_s > $R/rawcmp_S11_vs_c96rdS.txt; cat $R/rawcmp_S11_vs_c96rdS.txt; } ;;
  esac
done
echo RDALLDONE
