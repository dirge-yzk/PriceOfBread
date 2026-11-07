#!/usr/bin/env bash
cd /d/PoB_work/r931_work
for a in "keep_919/base_ins 1 base" "keep_919/n1 1 n1" "keep_919/n2 1 n2"; do
  set -- $a; echo "#### $1"; python -X utf8 -I read931b.py D:/PoB_work/$1 $2 $3 || exit 1
done
