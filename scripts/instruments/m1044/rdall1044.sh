#!/usr/bin/env bash
# (1044) 合流の読み（m1027/rdall1027.sh の型）：read1044.sh（門 c98lw/gates_lw.py・止める線 stoplines1002・an_h2 -v4・gdp_zero10・popjump・plvl・ukdef・nullgate・home_break・ng）を
#   M・S11・N1・N2 に回し、生の表ごとの sha1 を線の生と突き合わせる（M↔lw の腕 B・Z↔lc・da・lw・wr の旗 0 の ident）。
#   bash rdall1044.sh [腕のタグ:種の起点 ...]（既定＝mM_10:1 mS_10_s11:11 n1_10:1 n2_10:1）
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
T=D:/PoB_work/wt_m1044; S=$T/scripts/instruments; W=D:/PoB_work/m1044_work; R=$W/read; mkdir -p $R $W/hash
L=${*:-mM_10:1 mS_10_s11:11 n1_10:1 n2_10:1}
for a in $L; do tag=${a%%:*}; sb=${a##*:}; B=D:/PoB_work/keep_1035/base_ins; [ $sb = 11 ] && B=D:/PoB_work/keep_1027/a11
  echo "== $tag"; bash $S/m1044/read1044.sh $W $tag $R/$tag $sb $B; done
H="python -X utf8 -I $S/m1044/hash1044.py"; P="python -X utf8 -I $S/m1044/prefix1044.py"; HD=lordstat_s,tradeskip_s,warentry_s; KB=D:/PoB_work/keep_1035/base_ins
for a in $L; do tag=${a%%:*}
  case $tag in
    mM_10) [ -d $W/out_mM_10/ins ] && { $H $W/out_mM_10/ins $W/hash/mM.tsv $KB; $H --cmp $W/hash/lw_armB.tsv $W/hash/mM.tsv burscan_s,clock_s > $R/rawcmp_M_vs_c98lwB.txt
        $P $W/out_mM_10/ins $W/hash/pre_mM.tsv $KB $HD; $P --cmp $W/hash/pre_lwB.tsv $W/hash/pre_mM.tsv >> $R/rawcmp_M_vs_c98lwB.txt; cat $R/rawcmp_M_vs_c98lwB.txt; } ;;
  esac
done
Z=D:/PoB_work/ident_m1044z/out_ib_10/ins
if [ -d $Z ] && [ "${NOZ:-0}" != 1 ]; then $H $Z $W/hash/z.tsv $KB; $P $Z $W/hash/pre_z.tsv $KB $HD
  for t in lc da lwz wrz; do f=${t}_ib; [ $t = lwz ] && f=lwz_ib; [ $t = wrz ] && f=wrz_ib
    { $H --cmp $W/hash/$f.tsv $W/hash/z.tsv burscan_s,clock_s; $P --cmp $W/hash/pre_$t.tsv $W/hash/pre_z.tsv; } > $R/rawcmp_Z_vs_$t.txt; echo "== Z vs $t"; head -4 $R/rawcmp_Z_vs_$t.txt; done
fi
echo RDALLDONE
