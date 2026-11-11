#!/usr/bin/env bash
O=D:/PoB_work/r965_work/out
for a in "base_ins 1 base" "n1 1 n1" "n2 1 n2"; do
  set -- $a
  python -X utf8 -I D:/PoB_work/r965_work/read965d.py D:/PoB_work/keep_961/$1 $2 $3 $O > $O/read965d_$3.log 2>&1 || exit 1
done
