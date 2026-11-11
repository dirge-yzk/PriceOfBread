#!/usr/bin/env bash
# (826) 新しい基線 m826 の成績表と生の置き場（採ったときだけ）・(797) の fin797.sh の写し（腕の名と日付だけ m826 へ）。
cd D:/PoB_work/wt_m826; W=D:/PoB_work/m826; M=measurements
cp_b(){ for x in "" .cost .manifest; do cp "$W/$1$x" "$M/$2$x"; done; }
cp_b b10_mP_10.txt b10_20261104_m826
cp_b b5_mP5_5.txt b5_20261104_m826
cp_b b10_mB_10.txt b10_20261104_m826_ins
cp_b b10_mS_10_s11.txt b10s11_20261104_m826_ins
cp_b b10_n1_10.txt b10_20261104_m826_n1_ins
cp_b b10_n2_10.txt b10_20261104_m826_n2_ins
cp $W/mon_mB_10.txt $M/mon_20261104_m826.txt; cp $W/mon_n1_10.txt $M/mon_20261104_m826_n1.txt; cp $W/mon_n2_10.txt $M/mon_20261104_m826_n2.txt; cp $W/mon_mS_10_s11.txt $M/mon_20261104_m826s11.txt
K=D:/PoB_work/keep_826; mkdir -p $K
mv $W/out_mB_10/ins $K/base_ins; mv $W/out_n1_10/ins $K/n1; mv $W/out_n2_10/ins $K/n2; mv $W/out_mS_10_s11/ins $K/a11
echo FINDONE
