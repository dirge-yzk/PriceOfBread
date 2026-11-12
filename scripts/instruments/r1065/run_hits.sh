#!/usr/bin/env bash
S=D:/PoB_work/r1065_work/scripts/sz_hits_seats.py
O=D:/PoB_work/r1065_work/out
K=D:/PoB_work/keep_1053
M=C:/Users/dirge/Documents/PriceOfBread/measurements/m1059_20261112
for a in base n1 n2; do d=$a; [ $a = base ] && d=base_ins
python -X utf8 -I "$S" "$K/$d" 1 10 "$M/d1_${a}_s1_10.txt" "$O/hits_${a}.txt" $a || exit 1; done
