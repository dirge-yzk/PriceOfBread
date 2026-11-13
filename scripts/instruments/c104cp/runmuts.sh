#!/usr/bin/env bash
for m in M2 M3 M5; do
  git -C /d/PoB_work/c104cp_work/mut checkout -q -- .
  python -X utf8 -I /d/PoB_work/c104cp_work/muts.py $m
  bash /d/PoB_work/c104cp_work/runmut.sh $m
done
git -C /d/PoB_work/c104cp_work/mut checkout -q -- .
echo done > /d/PoB_work/c104cp_work/muts_all.mark
