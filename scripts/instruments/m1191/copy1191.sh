# (1191) 合流（m1184/copy1184.sh の型）：新しい基線 m1191 の成績表と腕の読みを measurements へ写す（生を keep_1191 へ移すのは別の命令＝同じドライブの名前の付け替え）。
#   bash copy1191.sh <リポジトリの木（wt_m1191）> base   ＝ M・S11・p5・p10・注視・NG・止める線の基準（M・S11）・読み
#   bash copy1191.sh <リポジトリの木（wt_m1191）> null   ＝ ヌル腕 N1 の成績表・止める線の基準（N1）・読み（ヌル腕は N1 の 1 本）
#   bash copy1191.sh <リポジトリの木（wt_m1191）> abl    ＝ アブレーションの成績表と読み（記録）
T=${1:?木}; P=${2:?base|null|abl}; W=D:/PoB_work/m1191_work; M=$T/measurements; D=20261118
mkdir -p $M/m1191_${D}/stopbase $M/m1191_${D}/read
if [ "$P" = base ]; then
cp $W/b5_mp5_5.txt $M/b5_${D}_m1191; cp $W/b5_mp5_5.txt.cost $M/b5_${D}_m1191.cost; cp $W/b5_mp5_5.txt.manifest $M/b5_${D}_m1191.manifest
cp $W/b10_mp10_10.txt $M/b10_${D}_m1191; cp $W/b10_mp10_10.txt.cost $M/b10_${D}_m1191.cost; cp $W/b10_mp10_10.txt.manifest $M/b10_${D}_m1191.manifest
cp $W/b10_mM_10.txt $M/b10_${D}_m1191_ins; cp $W/b10_mM_10.txt.cost $M/b10_${D}_m1191_ins.cost; cp $W/b10_mM_10.txt.manifest $M/b10_${D}_m1191_ins.manifest
cp $W/b10_mS11_10_s11.txt $M/b10s11_${D}_m1191_ins; cp $W/b10_mS11_10_s11.txt.cost $M/b10s11_${D}_m1191_ins.cost; cp $W/b10_mS11_10_s11.txt.manifest $M/b10s11_${D}_m1191_ins.manifest
cp $W/mon_mM_10.txt $M/mon_${D}_m1191.txt
mkdir -p $M/ng1191_${D}; for f in $W/read/M/ng/*_mM.txt; do b=$(basename $f _mM.txt); cp $f $M/ng1191_${D}/${b}_m1191.txt; done
for a in M S11; do cp $W/read/$a/stop1002.txt $M/m1191_${D}/stopbase/stop1002_$a.txt; mkdir -p $M/m1191_${D}/read/$a; cp $W/read/$a/*.txt $W/read/$a/*.json $M/m1191_${D}/read/$a/ 2>/dev/null; done
fi
if [ "$P" = null ]; then
cp $W/b10_mN1_10.txt $M/b10_${D}_m1191_n1_ins; cp $W/b10_mN1_10.txt.cost $M/b10_${D}_m1191_n1_ins.cost; cp $W/b10_mN1_10.txt.manifest $M/b10_${D}_m1191_n1_ins.manifest
cp $W/read/N1/stop1002.txt $M/m1191_${D}/stopbase/stop1002_N1.txt; mkdir -p $M/m1191_${D}/read/N1; cp $W/read/N1/*.txt $W/read/N1/*.json $M/m1191_${D}/read/N1/ 2>/dev/null
fi
if [ "$P" = abl ]; then
mkdir -p $M/m1191_${D}/abl
for a in Asec Amob Arev Aur; do [ -f $W/b10_m${a}_10.txt ] || continue
  cp $W/b10_m${a}_10.txt $W/b10_m${a}_10.txt.cost $W/b10_m${a}_10.txt.manifest $M/m1191_${D}/abl/
  mkdir -p $M/m1191_${D}/abl/$a; cp $W/read/$a/*.txt $W/read/$a/*.json $M/m1191_${D}/abl/$a/ 2>/dev/null; done
fi
cp $W/arms_start.txt $W/created.txt $M/m1191_${D}/ 2>/dev/null; [ -f $W/deleted.txt ] && cp $W/deleted.txt $M/m1191_${D}/; [ -f $W/moved.txt ] && cp $W/moved.txt $M/m1191_${D}/
echo COPYDONE
