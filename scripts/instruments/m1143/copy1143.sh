#!/usr/bin/env bash
# (1143) 合流：新しい基線 m1143 の成績表と腕の読みを measurements へ写し、生を keep_1143 へ移す（同じドライブ＝名前の付け替え）。
#   bash copy1143.sh <リポジトリの木（wt_m1143）>
T=${1:?木}; W=D:/PoB_work/m1143_work; M=$T/measurements; D=20261115
cp $W/b5_mp5_5.txt $M/b5_${D}_m1143; cp $W/b5_mp5_5.txt.cost $M/b5_${D}_m1143.cost; cp $W/b5_mp5_5.txt.manifest $M/b5_${D}_m1143.manifest
cp $W/b10_mp10_10.txt $M/b10_${D}_m1143; cp $W/b10_mp10_10.txt.cost $M/b10_${D}_m1143.cost; cp $W/b10_mp10_10.txt.manifest $M/b10_${D}_m1143.manifest
for a in "mM_10:_ins" "mN1_10:_n1_ins" "mN2_10:_n2_ins" "mAlo_10:_alo_ins" "mArb_10:_arb_ins" "mAtp_10:_atp_ins"; do t=${a%%:*}; s=${a#*:}
  cp $W/b10_$t.txt $M/b10_${D}_m1143$s; cp $W/b10_$t.txt.cost $M/b10_${D}_m1143$s.cost; cp $W/b10_$t.txt.manifest $M/b10_${D}_m1143$s.manifest; done
cp $W/b10_mS11_10_s11.txt $M/b10s11_${D}_m1143_ins; cp $W/b10_mS11_10_s11.txt.cost $M/b10s11_${D}_m1143_ins.cost; cp $W/b10_mS11_10_s11.txt.manifest $M/b10s11_${D}_m1143_ins.manifest
cp $W/b10_mS11z_10_s11.txt $M/b10s11_${D}_m1143z_lo0_ins; cp $W/b10_mS11z_10_s11.txt.cost $M/b10s11_${D}_m1143z_lo0_ins.cost; cp $W/b10_mS11z_10_s11.txt.manifest $M/b10s11_${D}_m1143z_lo0_ins.manifest
cp $W/mon_mM_10.txt $M/mon_${D}_m1143.txt
mkdir -p $M/ng1143_${D}; for f in $W/read/M/ng/*_mM.txt; do b=$(basename $f _mM.txt); cp $f $M/ng1143_${D}/${b}_m1143.txt; done
mkdir -p $M/m1143_${D}/stopbase $M/m1143_${D}/read
for a in M S11 N1 N2; do cp $W/read/$a/stop1002.txt $M/m1143_${D}/stopbase/stop1002_$a.txt; done
for a in M S11 Alo Arb Atp N1 N2; do mkdir -p $M/m1143_${D}/read/$a; cp $W/read/$a/*.txt $M/m1143_${D}/read/$a/ 2>/dev/null; done
cp $W/read/rec_add*.txt $M/m1143_${D}/ 2>/dev/null
cp $W/arms_start.txt $W/deleted.txt $W/created.txt $M/m1143_${D}/
echo COPYDONE
