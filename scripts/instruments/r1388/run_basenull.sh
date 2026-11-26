#!/usr/bin/env bash
S=D:/PoB_work/r1388_work/scripts; O=D:/PoB_work/r1388_work/out
for t in n1 n2; do
  python -X utf8 -I $S/uk_traj.py E:/PoB_archive/keep_1362/$t 1 $O/uk_b$t.tsv > $O/uk_b$t.log 2>&1; echo "uk b$t rc=$?"
  python -X utf8 -I $S/sec_read.py E:/PoB_archive/keep_1362/$t 1 b$t $O/sec_b$t.tsv > $O/sec_b$t.log 2>&1; echo "sec b$t rc=$?"
done
echo ALLDONE
