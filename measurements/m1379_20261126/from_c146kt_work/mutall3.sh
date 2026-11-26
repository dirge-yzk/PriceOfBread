#!/usr/bin/env bash
cd D:/PoB_work/c146kt_work/mut || exit 1
for k in l3 l3e; do git checkout -q -- .; PYTHONIOENCODING=utf-8 python -I D:/PoB_work/c146kt_work/mutl3.py $k; done
git checkout -q -- .
