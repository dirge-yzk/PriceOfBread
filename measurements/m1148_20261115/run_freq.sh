#!/usr/bin/env bash
cd /d/PoB_work/r1148_work
for a in "M base_ins" "N1 n1" "N2 n2" "S11 a11"; do set -- $a
  python -X utf8 -I scripts/instruments/r1148/freq.py D:/PoB_work/keep_1143/$2 $1 C:/Users/dirge/Documents/PriceOfBread > out_freq_$1.txt 2> out_freq_$1.err || echo "fail $1"
done
