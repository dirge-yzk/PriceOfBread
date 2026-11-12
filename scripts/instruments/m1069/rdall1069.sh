#!/usr/bin/env bash
# (1053) 合流の読み（m1044/rdall1044.sh の型）：read1053.sh を M・S11・N1・N2 に回し、生の表ごとの sha1 を線の生と突き合わせる（M↔db の腕 B・Z↔cd・db・ov の旗 0 の ident）。
#   bash rdall1053.sh [腕のタグ:種の起点 ...]（既定＝mM_10:1 mS_10_s11:11 n1_10:1 n2_10:1）
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
T=D:/PoB_work/wt_m1069; S=$T/scripts/instruments; W=D:/PoB_work/m1069_work; R=$W/read; mkdir -p $R $W/hash
L=${*:-mM_10:1 mS_10_s11:11 n1_10:1 n2_10:1}
for a in $L; do tag=${a%%:*}; sb=${a##*:}; B=D:/PoB_work/keep_1044/base_ins; [ $sb = 11 ] && B=D:/PoB_work/keep_1044/a11
  [ -d $W/out_$tag/ins ] || { echo "== $tag 無い"; continue; }
  echo "== $tag"; bash $S/m1053/read1053.sh $W $tag $R/$tag $sb $B; done
H="python -X utf8 -I $S/m1044/hash1044.py"; P="python -X utf8 -I $S/m1044/prefix1044.py"; HD=tradeskip_s,sellr_s,lordstat_s; KB=D:/PoB_work/keep_1044/base_ins
for a in $L; do tag=${a%%:*}
  case $tag in
    mM_10) [ -d $W/out_mM_10/ins ] && [ "${NOM:-0}" != 1 ] && { $H $W/out_mM_10/ins $W/hash/mM.tsv $KB; $P $W/out_mM_10/ins $W/hash/pre_mM.tsv $KB $HD
        { $H --cmp $W/hash/dbB.tsv $W/hash/mM.tsv burscan_s,clock_s; $P --cmp $W/hash/pre_dbB.tsv $W/hash/pre_mM.tsv; } > $R/rawcmp_M_vs_c99dbB.txt; head -4 $R/rawcmp_M_vs_c99dbB.txt; } ;;
  esac
done
Z=D:/PoB_work/ident_m1069z/out_ib_10/ins
if [ -d $Z ] && [ "${NOZ:-0}" != 1 ]; then $H $Z $W/hash/z.tsv $KB; $P $Z $W/hash/pre_z.tsv $KB $HD
  for t in cd dbz ovz; do { $H --cmp $W/hash/$t.tsv $W/hash/z.tsv burscan_s,clock_s; $P --cmp $W/hash/pre_$t.tsv $W/hash/pre_z.tsv; } > $R/rawcmp_Z_vs_$t.txt; echo "== Z vs $t"; head -4 $R/rawcmp_Z_vs_$t.txt; done
fi
echo RDALLDONE
