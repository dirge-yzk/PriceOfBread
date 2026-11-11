#!/usr/bin/env bash
cd /d/PoB_work/r916_work
for a in "keep_905_rej/c77cwB_10 1" "keep_905_rej/c77cwS_10_s11 11" "keep_905/base_ins 1" "keep_905/n1 1" "keep_905/n2 1" "keep_905/a11 11"; do
  set -- $a; echo "#### $1"; python -X utf8 -I deadreg916.py D:/PoB_work/$1 $2
done
