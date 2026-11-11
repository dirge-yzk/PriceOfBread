#!/usr/bin/env bash
# (839) 新しい基線 m839 の成績表と生の置き場（採ったときだけ）・(826) の fin826.sh の写し（腕の名と日付だけ m839 へ・M′＝mM2・S11′＝mS2）。
cd D:/PoB_work/wt_m839; W=D:/PoB_work/m839; M=measurements
cp_b(){ for x in "" .cost .manifest; do cp "$W/$1$x" "$M/$2$x"; done; }
cp_b b10_mP_10.txt b10_20261105_m839
cp_b b5_mP5_5.txt b5_20261105_m839
cp_b b10_mM2_10.txt b10_20261105_m839_ins
cp_b b10_mS2_10_s11.txt b10s11_20261105_m839_ins
cp_b b10_n1_10.txt b10_20261105_m839_n1_ins
cp_b b10_n2_10.txt b10_20261105_m839_n2_ins
cp $W/mon_mM2_10.txt $M/mon_20261105_m839.txt; cp $W/mon_n1_10.txt $M/mon_20261105_m839_n1.txt; cp $W/mon_n2_10.txt $M/mon_20261105_m839_n2.txt; cp $W/mon_mS2_10_s11.txt $M/mon_20261105_m839s11.txt
K=D:/PoB_work/keep_839; mkdir -p $K
mv $W/out_mM2_10/ins $K/base_ins; mv $W/out_n1_10/ins $K/n1; mv $W/out_n2_10/ins $K/n2; mv $W/out_mS2_10_s11/ins $K/a11
echo FINDONE
