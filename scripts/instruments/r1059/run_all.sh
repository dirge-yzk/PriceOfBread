#!/usr/bin/env bash
# (1059) 4 組の紙の数え（読むだけ）。呼び方：bash run_all.sh
S=D:/PoB_work/r1059_work/scripts/r1059_d1.py
O=D:/PoB_work/r1059_work/out
K=D:/PoB_work/keep_1053
python "$S" "$K/base_ins" 1 10 "$O/d1_base_s1_10.txt" base || exit 1
python "$S" "$K/n1" 1 10 "$O/d1_n1_s1_10.txt" n1 || exit 1
python "$S" "$K/n2" 1 10 "$O/d1_n2_s1_10.txt" n2 || exit 1
python "$S" "$K/a11" 11 20 "$O/d1_a11_s11_20.txt" a11 || exit 1
