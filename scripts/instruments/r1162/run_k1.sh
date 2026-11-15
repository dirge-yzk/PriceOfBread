#!/usr/bin/env bash
S=D:/PoB_work/r1162_work/scripts/r1162/k1_forms.py
O=D:/PoB_work/r1162_work/out
K=D:/PoB_work/keep_1157
python -X utf8 -I "$S" "$K/base_ins" 1 10 "$O/k1_base.txt" base || exit 1
python -X utf8 -I "$S" "$K/n1" 1 10 "$O/k1_n1.txt" n1 || exit 1
python -X utf8 -I "$S" "$K/n2" 1 10 "$O/k1_n2.txt" n2 || exit 1
echo done > "$O/k1.done"
