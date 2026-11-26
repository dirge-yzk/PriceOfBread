#!/usr/bin/env bash
# (1388) 読む線の全ての読み（腕 0 本）。出力 D:/PoB_work/r1388_work/out/
S=D:/PoB_work/r1388_work/scripts; O=D:/PoB_work/r1388_work/out; mkdir -p $O
export PYTHONUTF8=1
run() { tag=$1; dir=$2; sb=$3
  python -X utf8 -I $S/uk_traj.py $dir $sb $O/uk_$tag.tsv > $O/uk_$tag.log 2>&1; echo "uk $tag rc=$?"
  python -X utf8 -I $S/sec_read.py $dir $sb $tag $O/sec_$tag.tsv > $O/sec_$tag.log 2>&1; echo "sec $tag rc=$?"; }
run wyB D:/PoB_work/c146wy_work/armB/out_c146wyB_10/ins 1
run bkB D:/PoB_work/c146bk_work/armB/out_c146bkB_10/ins 1
run M D:/PoB_work/keep_1383/base_ins 1
run base E:/PoB_archive/keep_1362/base_ins 1
run N1 D:/PoB_work/keep_1383/n1 1
run N2 D:/PoB_work/keep_1383/n2 1
run S11 D:/PoB_work/keep_1383/a11 11
echo ALLDONE
