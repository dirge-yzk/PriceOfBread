#!/usr/bin/env bash
# (1150) 合流（m1143/copy1143.sh の型）：新しい基線 m1150 の成績表と腕の読みを measurements へ写す（生を keep_1150 へ移すのは別の命令＝同じドライブの名前の付け替え）。
#   bash copy1150.sh <リポジトリの木（wt_m1150）>
T=${1:?木}; W=D:/PoB_work/m1150_work; M=$T/measurements; D=20261115
cp $W/b5_mp5_5.txt $M/b5_${D}_m1150; cp $W/b5_mp5_5.txt.cost $M/b5_${D}_m1150.cost; cp $W/b5_mp5_5.txt.manifest $M/b5_${D}_m1150.manifest
cp $W/b10_mp10_10.txt $M/b10_${D}_m1150; cp $W/b10_mp10_10.txt.cost $M/b10_${D}_m1150.cost; cp $W/b10_mp10_10.txt.manifest $M/b10_${D}_m1150.manifest
for a in "mM_10:_ins" "mN1_10:_n1_ins" "mN2_10:_n2_ins"; do t=${a%%:*}; s=${a#*:}
  cp $W/b10_$t.txt $M/b10_${D}_m1150$s; cp $W/b10_$t.txt.cost $M/b10_${D}_m1150$s.cost; cp $W/b10_$t.txt.manifest $M/b10_${D}_m1150$s.manifest; done
cp $W/b10_mS11_10_s11.txt $M/b10s11_${D}_m1150_ins; cp $W/b10_mS11_10_s11.txt.cost $M/b10s11_${D}_m1150_ins.cost; cp $W/b10_mS11_10_s11.txt.manifest $M/b10s11_${D}_m1150_ins.manifest
cp $W/b10_mS11z_10_s11.txt $M/b10s11_${D}_m1150z_hy0_ins; cp $W/b10_mS11z_10_s11.txt.cost $M/b10s11_${D}_m1150z_hy0_ins.cost; cp $W/b10_mS11z_10_s11.txt.manifest $M/b10s11_${D}_m1150z_hy0_ins.manifest
cp $W/mon_mM_10.txt $M/mon_${D}_m1150.txt
mkdir -p $M/ng1150_${D}; for f in $W/read/M/ng/*_mM.txt; do b=$(basename $f _mM.txt); cp $f $M/ng1150_${D}/${b}_m1150.txt; done
mkdir -p $M/m1150_${D}/stopbase $M/m1150_${D}/read
for a in M S11 N1 N2; do cp $W/read/$a/stop1002.txt $M/m1150_${D}/stopbase/stop1002_$a.txt; done
for a in M S11 N1 N2; do mkdir -p $M/m1150_${D}/read/$a; cp $W/read/$a/*.txt $M/m1150_${D}/read/$a/ 2>/dev/null; done
cp $W/read/rec_*.txt $M/m1150_${D}/ 2>/dev/null
cp $W/read/suite_*.txt $W/read/guards_*.txt $W/rawcmp_M_hyB.tsv $M/m1150_${D}/ 2>/dev/null; mkdir -p $M/m1150_${D}/trx; cp $W/read/trx/*.trx $M/m1150_${D}/trx/
cp $W/arms_start.txt $W/deleted.txt $W/created.txt $M/m1150_${D}/
cp $W/hash/*.tsv $M/m1150_${D}/ 2>/dev/null; mkdir -p $M/m1150_${D}/hash; mv $M/m1150_${D}/*_trim.tsv $M/m1150_${D}/hash/ 2>/dev/null
echo COPYDONE
