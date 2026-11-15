#!/usr/bin/env bash
# (1157) 合流（m1150/copy1150.sh の型）：新しい基線 m1157 の成績表と腕の読みを measurements へ写す（生を keep_1157 へ移すのは別の命令＝同じドライブの名前の付け替え）。
#   bash copy1157.sh <リポジトリの木（wt_m1157）> base   ＝ M・S11・S11z・p5・p10・注視・NG・止める線の基準（M・S11）・読み
#   bash copy1157.sh <リポジトリの木（wt_m1157）> null   ＝ ヌル腕 N1・N2 の成績表・止める線の基準（N1・N2）・読み
T=${1:?木}; P=${2:?base|null}; W=D:/PoB_work/m1157_work; M=$T/measurements; D=20261115
mkdir -p $M/m1157_${D}/stopbase $M/m1157_${D}/read
if [ "$P" = base ]; then
cp $W/b5_mp5_5.txt $M/b5_${D}_m1157; cp $W/b5_mp5_5.txt.cost $M/b5_${D}_m1157.cost; cp $W/b5_mp5_5.txt.manifest $M/b5_${D}_m1157.manifest
cp $W/b10_mp10_10.txt $M/b10_${D}_m1157; cp $W/b10_mp10_10.txt.cost $M/b10_${D}_m1157.cost; cp $W/b10_mp10_10.txt.manifest $M/b10_${D}_m1157.manifest
cp $W/b10_mM_10.txt $M/b10_${D}_m1157_ins; cp $W/b10_mM_10.txt.cost $M/b10_${D}_m1157_ins.cost; cp $W/b10_mM_10.txt.manifest $M/b10_${D}_m1157_ins.manifest
cp $W/b10_mS11_10_s11.txt $M/b10s11_${D}_m1157_ins; cp $W/b10_mS11_10_s11.txt.cost $M/b10s11_${D}_m1157_ins.cost; cp $W/b10_mS11_10_s11.txt.manifest $M/b10s11_${D}_m1157_ins.manifest
cp $W/b10_mS11z_10_s11.txt $M/b10s11_${D}_m1157z_lc0_ins; cp $W/b10_mS11z_10_s11.txt.cost $M/b10s11_${D}_m1157z_lc0_ins.cost; cp $W/b10_mS11z_10_s11.txt.manifest $M/b10s11_${D}_m1157z_lc0_ins.manifest
cp $W/mon_mM_10.txt $M/mon_${D}_m1157.txt
mkdir -p $M/ng1157_${D}; for f in $W/read/M/ng/*_mM.txt; do b=$(basename $f _mM.txt); cp $f $M/ng1157_${D}/${b}_m1157.txt; done
for a in M S11; do cp $W/read/$a/stop1002.txt $M/m1157_${D}/stopbase/stop1002_$a.txt; mkdir -p $M/m1157_${D}/read/$a; cp $W/read/$a/*.txt $M/m1157_${D}/read/$a/ 2>/dev/null; done
fi
if [ "$P" = null ]; then
for a in "mN1_10:_n1_ins" "mN2_10:_n2_ins"; do t=${a%%:*}; s=${a#*:}
  cp $W/b10_$t.txt $M/b10_${D}_m1157$s; cp $W/b10_$t.txt.cost $M/b10_${D}_m1157$s.cost; cp $W/b10_$t.txt.manifest $M/b10_${D}_m1157$s.manifest; done
for a in N1 N2; do cp $W/read/$a/stop1002.txt $M/m1157_${D}/stopbase/stop1002_$a.txt; mkdir -p $M/m1157_${D}/read/$a; cp $W/read/$a/*.txt $M/m1157_${D}/read/$a/ 2>/dev/null; done
fi
cp $W/arms_start.txt $W/created.txt $M/m1157_${D}/ 2>/dev/null; [ -f $W/deleted.txt ] && cp $W/deleted.txt $M/m1157_${D}/; [ -f $W/moved.txt ] && cp $W/moved.txt $M/m1157_${D}/
echo COPYDONE
