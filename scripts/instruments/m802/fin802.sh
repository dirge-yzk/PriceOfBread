#!/usr/bin/env bash
# (802) 新しい基線 m802 の成績表と生の置き場（採ったときだけ）。
cd D:/PoB_work/wt_m802; W=D:/PoB_work/m802; M=measurements
cp_b(){ for x in "" .cost .manifest; do cp "$W/$1$x" "$M/$2$x"; done; }
cp_b b10_mP_10.txt b10_20261103_m802
cp_b b5_mP5_5.txt b5_20261103_m802
cp_b b10_mB_10.txt b10_20261103_m802_ins
cp_b b10_mS_10_s11.txt b10s11_20261103_m802_ins
cp_b b10_n1_10.txt b10_20261103_m802_n1_ins
cp_b b10_n2_10.txt b10_20261103_m802_n2_ins
cp $W/mon_mB_10.txt $M/mon_20261103_m802.txt; cp $W/mon_n1_10.txt $M/mon_20261103_m802_n1.txt; cp $W/mon_n2_10.txt $M/mon_20261103_m802_n2.txt; cp $W/mon_mS_10_s11.txt $M/mon_20261103_m802s11.txt
K=D:/PoB_work/keep_802; mkdir -p $K
mv $W/out_mB_10/ins $K/base_ins; mv $W/out_n1_10/ins $K/n1; mv $W/out_n2_10/ins $K/n2; mv $W/out_mS_10_s11/ins $K/a11
echo FINDONE
