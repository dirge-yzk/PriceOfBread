#!/usr/bin/env bash
S=D:/PoB_work/wt_c116k1/scripts/instruments/c116k1/paper_k1.py
O=D:/PoB_work/c116k1_work/paper
K=D:/PoB_work/keep_1171
mkdir -p $O
python -X utf8 -I "$S" "$K/base_ins" 1 10 "$O/k1_base.txt" base || exit 1
python -X utf8 -I "$S" "$K/n1" 1 10 "$O/k1_n1.txt" n1 || exit 1
python -X utf8 -I "$S" "$K/n2" 1 10 "$O/k1_n2.txt" n2 || exit 1
