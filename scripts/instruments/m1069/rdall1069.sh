#!/usr/bin/env bash
# (1069) 合流の読み（m1053/rdall1053.sh の型）：read1069.sh を M・S11・N1・N2 に回し、生の表ごとの sha1 を線の生と突き合わせる（M↔vs の腕 B・Z↔基準 keep_1053/base_ins・Z の dep_s↔vs の旗 0 の dep_s）。
#   bash rdall1069.sh [腕のタグ:種の起点 ...]（既定＝mM_10:1 mS_10_s11:11 n1_10:1 n2_10:1）
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
T=D:/PoB_work/wt_m1069; S=$T/scripts/instruments; W=D:/PoB_work/m1069_work; R=$W/read; mkdir -p $R $W/hash
Z=D:/PoB_work/ident_m1069z/out_ib_10/ins; KB=D:/PoB_work/keep_1053/base_ins
L=${*:-mM_10:1 mS_10_s11:11 n1_10:1 n2_10:1}
for a in $L; do tag=${a%%:*}; sb=${a##*:}; B=$KB; [ $sb = 11 ] && B=D:/PoB_work/keep_1053/a11
  [ -d $W/out_$tag/ins ] || { echo "== $tag 無い"; continue; }
  F0=-; [ $tag = mM_10 ] && F0=$Z; [ $tag = mS_10_s11 ] && F0=$W/out_$tag/ins
  echo "== $tag"; bash $S/m1069/read1069.sh $W $tag $R/$tag $sb $B $F0; done
H="python -X utf8 -I $S/m1044/hash1044.py"
for a in $L; do tag=${a%%:*}
  case $tag in
    mM_10) [ -d $W/out_mM_10/ins ] && [ "${NOM:-0}" != 1 ] && { $H $W/out_mM_10/ins $W/hash/mM.tsv $KB
        $H --cmp $W/hash/vsB.tsv $W/hash/mM.tsv burscan_s,clock_s > $R/rawcmp_M_vs_c101vsB.txt; head -4 $R/rawcmp_M_vs_c101vsB.txt; } ;;
  esac
done
if [ -d $Z ] && [ "${NOZ:-0}" != 1 ]; then $H $Z $W/hash/z.tsv $KB
  $H --cmp $T/measurements/m1053_20261112/hash/mM.tsv $W/hash/z.tsv burscan_s,clock_s > $R/rawcmp_Z_vs_base1053.txt; echo "== Z vs base1053"; head -4 $R/rawcmp_Z_vs_base1053.txt
  for s in 1 2 3 4 5 6 7 8 9 10; do cmp -s $Z/dep_s$s.tsv D:/PoB_work/c101vs_work/flag0_dep/dep_s$s.tsv && echo "dep_s$s 一致" || echo "dep_s$s 違い"; done > $R/depcmp_Z_vs_flag0.txt; cut -d' ' -f2 $R/depcmp_Z_vs_flag0.txt | sort | uniq -c
fi
echo RDALLDONE
