#!/usr/bin/env bash
O=D:/PoB_work/r1162_work/out
K=D:/PoB_work/keep_1157
P=D:/PoB_work/r1162_work/scripts/r1162
python -X utf8 -I "$P/r1059_d1_copy.py" "$K/base_ins" 1 10 "$O/d1_base.txt" base || exit 1
python -X utf8 -I "$P/soldier0.py" "$K/base_ins" 1 10 "$O/d1_base.txt" "$O/soldier0_base.txt" || exit 1
echo done > "$O/s0.done"
