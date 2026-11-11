#!/usr/bin/env bash
# (841) 段0：基線と新のヌル腕に an_h2 -v4 を当てる（読むだけ）
H=D:/PoB_work/wt_c68uh/scripts/instruments/hol/an_h2.py
for s in base_ins n1 n2; do python -X utf8 -I $H D:/PoB_work/keep_839/$s -v4 > D:/PoB_work/c68uh/anh2_$s.txt; done
