#!/usr/bin/env bash
# (1006) 合流：m1001 の 4 腕を stoplines1002 で数え直して基準を置く（読むだけ）
cd /c/Users/dirge/Documents/PriceOfBread
O=/d/PoB_work/m1006_work/stopbase; mkdir -p $O
bash scripts/instruments/c92gf/stoplines1002.sh D:/PoB_work/keep_1001/base_ins $O/stop1002_M.txt 1 || exit 11
bash scripts/instruments/c92gf/stoplines1002.sh D:/PoB_work/keep_1001/n1 $O/stop1002_N1.txt 1 || exit 12
bash scripts/instruments/c92gf/stoplines1002.sh D:/PoB_work/keep_1001/n2 $O/stop1002_N2.txt 1 || exit 13
bash scripts/instruments/c92gf/stoplines1002.sh D:/PoB_work/keep_1001/a11 $O/stop1002_S11.txt 11 || exit 14
PYTHONUTF8=1 python -X utf8 -I scripts/instruments/c92gf/gdp_jump1002.py --rows N2988=D:/PoB_work/keep_988/n2 > $O/siam_m988n2_fresh.txt || exit 15
echo done
