#!/usr/bin/env bash
S=D:/PoB_work/wt_c116k1/scripts/instruments/c116k1/gates_k1.py
O=D:/PoB_work/c116k1_work/paper
for t in base_ins n1 n2; do python -X utf8 -I $S D:/PoB_work/keep_1171/$t 1 10 $O/gates_$t.txt $t > /dev/null || exit 1; done
