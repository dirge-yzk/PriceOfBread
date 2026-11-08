#!/usr/bin/env bash
O=D:/PoB_work/r965_work/out
for a in "base_ins base" "n1 n1" "n2 n2"; do
  set -- $a
  python -X utf8 -I D:/PoB_work/r965_work/read965f.py D:/PoB_work/keep_961/$1 $2 $O > $O/read965f_$2.log 2>&1 || exit 1
done
