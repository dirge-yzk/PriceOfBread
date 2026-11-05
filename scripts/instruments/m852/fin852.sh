#!/usr/bin/env bash
# (852) 新しい基線 m852 の成績表と記録の置き場（(839) の fin839.sh の写し・腕の名と日付だけ m852 へ・M＝mM・S11＝mS・素の 10 種＝mP・素の 5 種＝mP5）。
#   生は既に keep_852/base_ins（M）・n1・n2・a11（門と止める線の 8 表だけ）へ置いた（shrinkrej852.sh）。ここは成績表・.cost・.manifest・注視の表と読み手の出力を measurements へ写す。
cd D:/PoB_work/wt_m852; W=D:/PoB_work/m852; M=measurements; O=$M/m852_20261105
cp_b(){ for x in "" .cost .manifest; do cp "$W/$1$x" "$M/$2$x"; done; }
cp_b b10_mP_10.txt b10_20261105_m852
cp_b b5_mP5_5.txt b5_20261105_m852
cp_b b10_mM_10.txt b10_20261105_m852_ins
cp_b b10_mS_10_s11.txt b10s11_20261105_m852_ins
cp_b b10_n1_10.txt b10_20261105_m852_n1_ins
cp_b b10_n2_10.txt b10_20261105_m852_n2_ins
cp $W/mon_mM_10.txt $M/mon_20261105_m852.txt; cp $W/mon_n1_10.txt $M/mon_20261105_m852_n1.txt; cp $W/mon_n2_10.txt $M/mon_20261105_m852_n2.txt; cp $W/mon_mS_10_s11.txt $M/mon_20261105_m852s11.txt
mkdir -p $O/armM $O/armS11 $O/null
cp $W/readM/* $O/armM/; cp $W/readS/* $O/armS11/; cp $W/readN/* $O/null/; cp -r $W/hol847S $O/armS11/hol847
cp $W/m_eq_b2_known.txt $O/ 2>/dev/null
K=D:/PoB_work/keep_852
for d in a11 n1 n2; do [ -d $K/${d}_tmp ] && mv $K/${d}_tmp $K/${d}_report; done
echo FINDONE
