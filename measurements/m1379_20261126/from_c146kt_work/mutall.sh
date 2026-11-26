#!/usr/bin/env bash
cd D:/PoB_work/c146kt_work/mut || exit 1
for k in mu1 mu3 m1b pf l3 l3e l5 bare defdrop; do
  git checkout -q -- . 
  python -I D:/PoB_work/c146kt_work/mut.py $k
done
git checkout -q -- .
