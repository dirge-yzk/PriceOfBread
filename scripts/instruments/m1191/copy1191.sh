# (1191) 合流（m1184/copy1184.sh の写し・木と作業フォルダを wt_m1191・m1191_work へ）
# (1184) 合流（m1178/copy1178.sh の写し・作業フォルダ・名を m1184 へ・S11z の名は m1184z_hd05〔旗 0＝StockpilePanicWeight 0.5〕・下の行の m1184 は写しの置き換え＝元は m1178）
# (1178) 合流（m1171/copy1171.sh の写し・作業フォルダ・名を m1184 へ・S11z の名は m1184z_hd05〔旗 0＝TradeBuyBySupplyCurve 0〕・下の行の m1184 は写しの置き換え＝元は m1171）
# (1171) 合流（m1164/copy1164.sh の写し・木と作業フォルダ・名を m1184・日付 20261116 へ・S11z の名は m1184z_hd05〔旗 0＝SoldierVacancyFromZero 0〕・下の行の m1184 は写しの置き換え＝元は m1164）
# (1164) 合流（m1157/copy1157.sh の写し・木と作業フォルダ・名を m1184 へ・下の行の m1184 は写しの置き換え＝元は m1157）
# (1157) 合流（m1150/copy1150.sh の型）：新しい基線 m1184 の成績表と腕の読みを measurements へ写す（生を keep_1157 へ移すのは別の命令＝同じドライブの名前の付け替え）。
#   bash copy1157.sh <リポジトリの木（wt_m1191）> base   ＝ M・S11・S11z・p5・p10・注視・NG・止める線の基準（M・S11）・読み
#   bash copy1157.sh <リポジトリの木（wt_m1191）> null   ＝ ヌル腕 N1・N2 の成績表・止める線の基準（N1・N2）・読み
T=${1:?木}; P=${2:?base|null}; W=D:/PoB_work/m1191_work; M=$T/measurements; D=20261116
mkdir -p $M/m1184_${D}/stopbase $M/m1184_${D}/read
if [ "$P" = base ]; then
cp $W/b5_mp5_5.txt $M/b5_${D}_m1184; cp $W/b5_mp5_5.txt.cost $M/b5_${D}_m1184.cost; cp $W/b5_mp5_5.txt.manifest $M/b5_${D}_m1184.manifest
cp $W/b10_mp10_10.txt $M/b10_${D}_m1184; cp $W/b10_mp10_10.txt.cost $M/b10_${D}_m1184.cost; cp $W/b10_mp10_10.txt.manifest $M/b10_${D}_m1184.manifest
cp $W/b10_mM_10.txt $M/b10_${D}_m1184_ins; cp $W/b10_mM_10.txt.cost $M/b10_${D}_m1184_ins.cost; cp $W/b10_mM_10.txt.manifest $M/b10_${D}_m1184_ins.manifest
cp $W/b10_mS11_10_s11.txt $M/b10s11_${D}_m1184_ins; cp $W/b10_mS11_10_s11.txt.cost $M/b10s11_${D}_m1184_ins.cost; cp $W/b10_mS11_10_s11.txt.manifest $M/b10s11_${D}_m1184_ins.manifest
cp $W/b10_mS11z_10_s11.txt $M/b10s11_${D}_m1184z_hd05_ins; cp $W/b10_mS11z_10_s11.txt.cost $M/b10s11_${D}_m1184z_hd05_ins.cost; cp $W/b10_mS11z_10_s11.txt.manifest $M/b10s11_${D}_m1184z_hd05_ins.manifest
cp $W/mon_mM_10.txt $M/mon_${D}_m1184.txt
mkdir -p $M/ng1184_${D}; for f in $W/read/M/ng/*_mM.txt; do b=$(basename $f _mM.txt); cp $f $M/ng1184_${D}/${b}_m1184.txt; done
for a in M S11; do cp $W/read/$a/stop1002.txt $M/m1184_${D}/stopbase/stop1002_$a.txt; mkdir -p $M/m1184_${D}/read/$a; cp $W/read/$a/*.txt $M/m1184_${D}/read/$a/ 2>/dev/null; done
fi
if [ "$P" = null ]; then
for a in "mN1_10:_n1_ins" "mN2_10:_n2_ins"; do t=${a%%:*}; s=${a#*:}
  cp $W/b10_$t.txt $M/b10_${D}_m1184$s; cp $W/b10_$t.txt.cost $M/b10_${D}_m1184$s.cost; cp $W/b10_$t.txt.manifest $M/b10_${D}_m1184$s.manifest; done
for a in N1 N2; do cp $W/read/$a/stop1002.txt $M/m1184_${D}/stopbase/stop1002_$a.txt; mkdir -p $M/m1184_${D}/read/$a; cp $W/read/$a/*.txt $M/m1184_${D}/read/$a/ 2>/dev/null; done
fi
cp $W/arms_start.txt $W/created.txt $M/m1184_${D}/ 2>/dev/null; [ -f $W/deleted.txt ] && cp $W/deleted.txt $M/m1184_${D}/; [ -f $W/moved.txt ] && cp $W/moved.txt $M/m1184_${D}/
echo COPYDONE
