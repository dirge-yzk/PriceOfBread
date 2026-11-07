#!/usr/bin/env bash
export PYTHONIOENCODING=utf-8
K=D:/PoB_work/keep_933; O=/d/PoB_work/c83ic_work
cd /d/PoB_work/wt_c83ic
for m in M0 NEW M1 M2 M3 M4 M5 M6; do
  python scripts/instruments/c83ic/paper943.py $K/base_ins $K/n1 $K/n2 $K/a11 --mut $m > $O/paper_$m.txt 2>&1 || exit 1
done
