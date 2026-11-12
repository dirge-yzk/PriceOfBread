#!/usr/bin/env bash
# (1065) 兵 0 の数え（読むだけ）。呼び方：bash run_all.sh
S=D:/PoB_work/r1065_work/scripts/sz_count.py
O=D:/PoB_work/r1065_work/out
K=D:/PoB_work/keep_1053
M=C:/Users/dirge/Documents/PriceOfBread/measurements/m1059_20261112
python -X utf8 -I "$S" "$K/base_ins" 1 3 "$O/sz_base_s1_3.txt" base - || exit 1
python -X utf8 -I "$S" "$K/base_ins" 1 10 "$O/sz_base_s1_10.txt" base "$M/d1_base_s1_10.txt" || exit 1
python -X utf8 -I "$S" "$K/n1" 1 10 "$O/sz_n1_s1_10.txt" n1 "$M/d1_n1_s1_10.txt" || exit 1
python -X utf8 -I "$S" "$K/n2" 1 10 "$O/sz_n2_s1_10.txt" n2 "$M/d1_n2_s1_10.txt" || exit 1
python -X utf8 -I "$S" "$K/a11" 11 20 "$O/sz_a11_s11_20.txt" a11 "$M/d1_a11_s11_20.txt" || exit 1
